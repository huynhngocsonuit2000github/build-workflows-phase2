using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using WorkflowApi.Api.Data;
using WorkflowApi.Api.Endpoints;
using WorkflowApi.Domain;

namespace WorkflowApi.Api.Tests;

/// <summary>
/// Bằng chứng cho 5 tiêu chí hoàn thành của bước 1 (s1-5), mỗi tiêu chí một test, đi qua API thật + PostgreSQL thật.
/// Chạy riêng: dotnet test --filter "FullyQualifiedName~Step1CriteriaTests"
/// Các ca chi tiết hơn nằm ở ExecutionEndpointsTests, ConcurrencyTests, IdempotencyTests, RetryAndListTests.
/// </summary>
[Collection("postgres")]
public class Step1CriteriaTests(PostgresFixture postgres) : IAsyncLifetime
{
    private WorkflowApiFactory _factory = null!;

    public async Task InitializeAsync() => _factory = await WorkflowApiFactory.CreateAsync(postgres);

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Criterion1_StateIsPersisted_ReadBackByIndependentDbConnection()
    {
        var created = await _factory.CreateExecutionAsync("TAS-4");
        var moved = await _factory.AdvanceAsync(created, ("RequirementAnalysis", "N8n"));

        // Kết nối mới, không qua API, không qua cache của app: dữ liệu thật sự nằm trong PostgreSQL.
        var options = new DbContextOptionsBuilder<WorkflowDbContext>().UseNpgsql(_factory.ConnectionString).Options;
        await using var db = new WorkflowDbContext(options);
        var row = await db.Executions.AsNoTracking().SingleAsync(e => e.Id == created.Id);

        Assert.Equal(WorkflowState.RequirementAnalysis, row.CurrentState);
        Assert.Equal(moved.Version, row.Version);
        Assert.Equal(2, row.History.Count);
    }

    [Fact]
    public async Task Criterion2_InvalidTransitionsAreRejected_AndNothingIsWritten()
    {
        var created = await _factory.CreateExecutionAsync();

        var skip = await _factory.TransitionAsync(created.Id, "Coding", "N8n", created.Version);          // nhảy bước
        var wrongActor = await _factory.TransitionAsync(created.Id, "RequirementAnalysis", "Agent", created.Version); // sai người
        var stale = await _factory.TransitionAsync(created.Id, "RequirementAnalysis", "N8n", created.Version + 5);   // version cũ/sai

        Assert.Equal(HttpStatusCode.UnprocessableEntity, skip.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, wrongActor.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);

        var after = await _factory.GetAsync(created.Id);
        Assert.Equal(("Created", created.Version), (after.CurrentState, after.Version));
        Assert.Single(await _factory.GetHistoryAsync(created.Id));
    }

    [Fact]
    public async Task Criterion3_DuplicateEventsDoNotCreateDuplicateRuns()
    {
        var body = new { jiraIssueKey = "TAS-7", actor = new { type = "N8n", id = "n8n" } };

        // Cùng một sự kiện Jira gửi 5 lần cùng lúc + 1 lần gửi lại sau đó.
        var parallel = await Task.WhenAll(
            Enumerable.Range(0, 5).Select(_ => _factory.PostExecutionAsync(body, "jira:TAS-7:evt-42")));
        var later = await _factory.PostExecutionAsync(body, "jira:TAS-7:evt-42");

        var ids = new List<Guid>();
        foreach (var res in parallel.Append(later))
        {
            Assert.Equal(HttpStatusCode.Created, res.StatusCode);
            ids.Add((await res.Content.ReadFromJsonAsync<ExecutionResponse>())!.Id);
        }

        Assert.Single(ids.Distinct());
        Assert.Equal(1, await _factory.CountExecutionsAsync("TAS-7"));
    }

    [Fact]
    public async Task Criterion4_HistoryIsInspectable_InOrderWithActorAndReason()
    {
        var created = await _factory.CreateExecutionAsync();
        await _factory.AdvanceAsync(created, ("RequirementAnalysis", "N8n"), ("AwaitingRequirementApproval", "Agent"));

        var history = await _factory.GetHistoryAsync(created.Id);

        Assert.Equal(
            [(null, "Created"), ("Created", "RequirementAnalysis"), ("RequirementAnalysis", "AwaitingRequirementApproval")],
            history.Select(h => (h.FromState, h.ToState)));
        Assert.All(history, h => Assert.False(string.IsNullOrWhiteSpace(h.ChangedBy)));
        Assert.Equal("N8n:tester", history[1].ChangedBy);
    }

    [Fact]
    public async Task Criterion5_FailedRunCanBeFoundAndSafelyRetried_WithoutCreatingANewRun()
    {
        var failed = await _factory.CreateFailedAtCodingAsync("TAS-8");
        await _factory.CreateExecutionAsync("TAS-5"); // một lần chạy bình thường, không được lẫn vào

        // Tìm được
        var found = Assert.Single(await _factory.ListAsync("state=Failed"));
        Assert.Equal(failed.Id, found.Id);
        Assert.Equal("Coding", found.FailedFromState);

        // Retry an toàn: gửi 2 lần (giả lập n8n retry vì timeout) -> chỉ 1 lần có hiệu lực
        var countBefore = await _factory.CountAllExecutionsAsync();
        var first = await _factory.RetryAsync(found.Id, "Human", found.Version);
        var again = await _factory.RetryAsync(found.Id, "Human", found.Version);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal(countBefore, await _factory.CountAllExecutionsAsync()); // không có lần chạy mới
        Assert.Equal("Coding", (await _factory.GetAsync(failed.Id)).CurrentState);
        Assert.Empty(await _factory.ListAsync("state=Failed"));
        Assert.Single(await _factory.GetHistoryAsync(failed.Id), h => h.FromState == "Failed");
    }
}
