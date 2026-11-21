using System.Net;
using System.Net.Http.Json;
using WorkflowApi.Api.Endpoints;

namespace WorkflowApi.Api.Tests;

[Collection("postgres")]
public class IdempotencyTests(PostgresFixture postgres) : IAsyncLifetime
{
    private WorkflowApiFactory _factory = null!;

    public async Task InitializeAsync() => _factory = await WorkflowApiFactory.CreateAsync(postgres);

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private static object Body(string issue = "TAS-7", string actorId = "n8n") => new
    {
        jiraIssueKey = issue,
        correlationId = "corr-7",
        actor = new { type = "N8n", id = actorId }
    };

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateExecution_MissingIdempotencyKey_Returns400AndCreatesNothing(string? key)
    {
        var res = await _factory.PostExecutionAsync(Body(), key);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal(0, await _factory.CountExecutionsAsync("TAS-7"));
    }

    [Fact]
    public async Task CreateExecution_KeyTooLong_Returns400()
    {
        var res = await _factory.PostExecutionAsync(Body(), new string('k', 201));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    /// <summary>Kịch bản 1 ở s1-1: webhook Jira đến hai lần.</summary>
    [Fact]
    public async Task CreateExecution_SameKeyTwice_ReturnsSameExecutionAndCreatesOnlyOne()
    {
        var first = await _factory.PostExecutionAsync(Body(), "jira:TAS-7:evt-100");
        var second = await _factory.PostExecutionAsync(Body(), "jira:TAS-7:evt-100");

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.False(first.Headers.Contains("Idempotency-Replayed"));
        Assert.Equal("true", second.Headers.GetValues("Idempotency-Replayed").Single());

        var firstBody = await first.Content.ReadAsStringAsync();
        var secondBody = await second.Content.ReadAsStringAsync();
        Assert.Equal(firstBody, secondBody); // trả lại nguyên văn
        Assert.Equal(first.Headers.Location, second.Headers.Location);
        Assert.Equal(1, await _factory.CountExecutionsAsync("TAS-7"));
    }

    /// <summary>
    /// Giả lập timeout: lần đầu server đã tạo xong nhưng client không nhận được phản hồi,
    /// client gửi lại cùng key sau khi lần chạy đã đi tiếp vài bước.
    /// Lần gửi lại phải trả kết quả GỐC (lúc tạo), không tạo lần chạy mới.
    /// </summary>
    [Fact]
    public async Task CreateExecution_RetryAfterTimeoutWhenRunMovedOn_ReturnsOriginalResultNoNewRun()
    {
        var first = await _factory.PostExecutionAsync(Body(), "jira:TAS-7:evt-200");
        var created = (await first.Content.ReadFromJsonAsync<ExecutionResponse>())!;
        await _factory.AdvanceAsync(created, ("RequirementAnalysis", "N8n"));

        var retry = await _factory.PostExecutionAsync(Body(), "jira:TAS-7:evt-200");

        Assert.Equal(HttpStatusCode.Created, retry.StatusCode);
        var replayed = (await retry.Content.ReadFromJsonAsync<ExecutionResponse>())!;
        Assert.Equal(created.Id, replayed.Id);
        Assert.Equal("Created", replayed.CurrentState); // kết quả gốc của thao tác tạo
        Assert.Equal("RequirementAnalysis", (await _factory.GetAsync(created.Id)).CurrentState); // trạng thái thật vẫn đúng
        Assert.Equal(1, await _factory.CountExecutionsAsync("TAS-7"));
    }

    [Fact]
    public async Task CreateExecution_SameKeyDifferentBody_Returns422AndCreatesNothingNew()
    {
        var first = await _factory.PostExecutionAsync(Body("TAS-7"), "jira:evt-300");
        var reused = await _factory.PostExecutionAsync(Body("TAS-8"), "jira:evt-300");

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, reused.StatusCode);
        Assert.Equal("IdempotencyKeyReused", await WorkflowApiFactory.ErrorCodeAsync(reused));
        Assert.Equal(0, await _factory.CountExecutionsAsync("TAS-8"));
    }

    [Fact]
    public async Task CreateExecution_SameKeyBodyDiffersOnlyByWhitespace_TreatedAsSameRequest()
    {
        var first = await _factory.PostExecutionAsync(Body("TAS-7"), "jira:evt-400");
        var second = await _factory.PostExecutionAsync(Body("  TAS-7  "), "jira:evt-400");

        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.Equal("true", second.Headers.GetValues("Idempotency-Replayed").Single());
        Assert.Equal(1, await _factory.CountExecutionsAsync("TAS-7"));
    }

    [Fact]
    public async Task CreateExecution_DifferentKeysSameIssue_CreatesTwoRuns()
    {
        // Hai sự kiện khác nhau (ví dụ ticket được mở lại) là hai lần chạy hợp lệ: key quyết định, không phải issue.
        await _factory.PostExecutionAsync(Body(), "jira:TAS-7:evt-500");
        await _factory.PostExecutionAsync(Body(), "jira:TAS-7:evt-501");

        Assert.Equal(2, await _factory.CountExecutionsAsync("TAS-7"));
    }

    /// <summary>
    /// Hai lần poll chồng nhau: nhiều request cùng key đến cùng lúc.
    /// Tất cả nhận cùng một execution; DB chỉ có một.
    /// </summary>
    [Fact]
    public async Task CreateExecution_ParallelSameKey_AllGetSameExecutionAndOnlyOneExists()
    {
        const int rounds = 10;
        const int parallelRequests = 8;

        for (var round = 0; round < rounds; round++)
        {
            var issue = $"TAS-P{round}";
            var key = $"jira:{issue}:evt-race";

            var responses = await Task.WhenAll(Enumerable.Range(0, parallelRequests)
                .Select(_ => _factory.PostExecutionAsync(Body(issue), key)));

            Assert.All(responses, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));
            var ids = new HashSet<Guid>();
            foreach (var r in responses)
            {
                ids.Add((await r.Content.ReadFromJsonAsync<ExecutionResponse>())!.Id);
            }

            Assert.Single(ids);
            Assert.Equal(1, await _factory.CountExecutionsAsync(issue));
            Assert.Equal(parallelRequests - 1, responses.Count(r => r.Headers.Contains("Idempotency-Replayed")));
        }
    }
}
