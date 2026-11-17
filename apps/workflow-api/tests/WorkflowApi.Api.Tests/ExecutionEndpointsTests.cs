using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using WorkflowApi.Api.Endpoints;

namespace WorkflowApi.Api.Tests;

[Collection("postgres")]
public class ExecutionEndpointsTests(PostgresFixture postgres) : IAsyncLifetime
{
    private WorkflowApiFactory _factory = null!;

    public async Task InitializeAsync() => _factory = await WorkflowApiFactory.CreateAsync(postgres);

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    // ---- Health ----

    [Fact]
    public async Task GetHealth_NoAuth_Returns200Ok()
    {
        var res = await _factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    // ---- Tạo ----

    [Fact]
    public async Task CreateExecution_ValidRequest_Returns201AtCreatedVersion1()
    {
        var res = await _factory.CreateClient().PostAsJsonAsync("/executions", new
        {
            jiraIssueKey = "TAS-5",
            correlationId = "corr-1",
            actor = new { type = "N8n", id = "n8n" }
        });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<ExecutionResponse>();
        Assert.NotNull(body);
        Assert.Equal("Created", body.CurrentState);
        Assert.Equal(1, body.Version);
        Assert.Equal("TAS-5", body.JiraIssueKey);
        Assert.Equal("corr-1", body.CorrelationId);
        Assert.Equal($"/executions/{body.Id}", res.Headers.Location?.ToString());
    }

    [Theory]
    [InlineData("", "N8n", "n8n")]
    [InlineData("   ", "N8n", "n8n")]
    [InlineData("TAS-5", "Robot", "x")]
    [InlineData("TAS-5", "1", "x")]
    [InlineData("TAS-5", "N8n", "")]
    public async Task CreateExecution_InvalidInput_Returns400(string key, string actorType, string actorId)
    {
        var res = await _factory.CreateClient().PostAsJsonAsync("/executions", new
        {
            jiraIssueKey = key,
            actor = new { type = actorType, id = actorId }
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    // ---- Đọc trạng thái và lịch sử ----

    [Fact]
    public async Task GetExecution_Existing_ReturnsPersistedState()
    {
        var created = await _factory.CreateExecutionAsync();

        var fetched = await _factory.GetAsync(created.Id);

        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal("Created", fetched.CurrentState);
        Assert.Equal(1, fetched.Version);
    }

    [Fact]
    public async Task GetExecution_Unknown_Returns404ProblemDetails()
    {
        var res = await _factory.CreateClient().GetAsync($"/executions/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        Assert.Equal("application/problem+json", res.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetHistory_Unknown_Returns404()
    {
        var res = await _factory.CreateClient().GetAsync($"/executions/{Guid.NewGuid()}/history");

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task GetHistory_NewExecution_HasOneCreatedRow()
    {
        var created = await _factory.CreateExecutionAsync();

        var history = await _factory.GetHistoryAsync(created.Id);

        var row = Assert.Single(history);
        Assert.Null(row.FromState);
        Assert.Equal("Created", row.ToState);
        Assert.Equal("N8n:n8n", row.ChangedBy);
    }

    [Fact]
    public async Task GetHistory_AfterTransitions_ReturnsRowsInOrderWithActorAndReason()
    {
        var created = await _factory.CreateExecutionAsync();
        var analysis = await _factory.AdvanceAsync(created, ("RequirementAnalysis", "N8n"));
        var res = await _factory.TransitionAsync(
            analysis.Id, "AwaitingRequirementApproval", "Agent", analysis.Version, "analysis ready", "ba-agent");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var history = await _factory.GetHistoryAsync(created.Id);

        Assert.Equal(3, history.Count);
        Assert.Equal(["Created", "RequirementAnalysis", "AwaitingRequirementApproval"], history.Select(h => h.ToState));
        Assert.Equal("RequirementAnalysis", history[2].FromState);
        Assert.Equal("Agent:ba-agent", history[2].ChangedBy);
        Assert.Equal("analysis ready", history[2].Reason);
    }

    // ---- Chuyển trạng thái ----

    [Fact]
    public async Task Transition_ValidStep_Returns200AndPersistsNewVersion()
    {
        var created = await _factory.CreateExecutionAsync();

        var res = await _factory.TransitionAsync(created.Id, "RequirementAnalysis", "N8n", created.Version);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var fetched = await _factory.GetAsync(created.Id);
        Assert.Equal("RequirementAnalysis", fetched.CurrentState);
        Assert.Equal(2, fetched.Version);
    }

    [Fact]
    public async Task Transition_HappyPath_ReachesCompletedAndKeepsFullHistory()
    {
        var created = await _factory.CreateExecutionAsync();

        var done = await _factory.AdvanceAsync(
            created,
            ("RequirementAnalysis", "N8n"),
            ("AwaitingRequirementApproval", "Agent"),
            ("ImplementationPlanning", "Human"),
            ("Coding", "Agent"),
            ("BuildAndTest", "Agent"),
            ("CodeReview", "Ci"),
            ("AwaitingCodeApproval", "Agent"),
            ("PullRequestCreated", "Human"),
            ("ReadyForDeployment", "Ci"),
            ("Completed", "Ci"));

        Assert.Equal("Completed", done.CurrentState);
        Assert.Equal(11, done.Version);
        Assert.Equal(11, (await _factory.GetHistoryAsync(created.Id)).Count);
    }

    [Fact]
    public async Task Transition_AgentApprovesRequirement_Returns403AndNothingChanges()
    {
        var created = await _factory.CreateExecutionAsync();
        var waiting = await _factory.AdvanceAsync(
            created, ("RequirementAnalysis", "N8n"), ("AwaitingRequirementApproval", "Agent"));

        var res = await _factory.TransitionAsync(waiting.Id, "ImplementationPlanning", "Agent", waiting.Version);

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        Assert.Equal("ActorNotAllowed", await WorkflowApiFactory.ErrorCodeAsync(res));
        var fetched = await _factory.GetAsync(waiting.Id);
        Assert.Equal("AwaitingRequirementApproval", fetched.CurrentState);
        Assert.Equal(waiting.Version, fetched.Version);
        Assert.Equal(3, (await _factory.GetHistoryAsync(waiting.Id)).Count);
    }

    [Fact]
    public async Task Transition_SkipStep_Returns422NotAllowed()
    {
        var created = await _factory.CreateExecutionAsync();

        var res = await _factory.TransitionAsync(created.Id, "Coding", "Agent", created.Version);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, res.StatusCode);
        Assert.Equal("NotAllowed", await WorkflowApiFactory.ErrorCodeAsync(res));
    }

    [Fact]
    public async Task Transition_StaleVersion_Returns409VersionConflict()
    {
        var created = await _factory.CreateExecutionAsync();
        await _factory.AdvanceAsync(created, ("RequirementAnalysis", "N8n"));

        var res = await _factory.TransitionAsync(created.Id, "RequirementAnalysis", "N8n", created.Version);

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("VersionConflict", await WorkflowApiFactory.ErrorCodeAsync(res));
    }

    [Fact]
    public async Task Transition_SameRequestTwice_SecondIs409AndStateChangedOnce()
    {
        var created = await _factory.CreateExecutionAsync();

        var first = await _factory.TransitionAsync(created.Id, "RequirementAnalysis", "N8n", created.Version);
        var second = await _factory.TransitionAsync(created.Id, "RequirementAnalysis", "N8n", created.Version);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal(2, (await _factory.GetAsync(created.Id)).Version);
        Assert.Equal(2, (await _factory.GetHistoryAsync(created.Id)).Count);
    }

    [Fact]
    public async Task Transition_FixAttemptsExhausted_Returns422GuardFailed()
    {
        var created = await _factory.CreateExecutionAsync();
        var current = await _factory.AdvanceAsync(
            created,
            ("RequirementAnalysis", "N8n"),
            ("AwaitingRequirementApproval", "Agent"),
            ("ImplementationPlanning", "Human"),
            ("Coding", "Agent"),
            ("BuildAndTest", "Agent"));

        for (var i = 0; i < 3; i++)
        {
            current = await _factory.AdvanceAsync(current, ("Coding", "Ci"), ("BuildAndTest", "Agent"));
        }

        var res = await _factory.TransitionAsync(current.Id, "Coding", "Ci", current.Version);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, res.StatusCode);
        Assert.Equal("GuardFailed", await WorkflowApiFactory.ErrorCodeAsync(res));
    }

    [Fact]
    public async Task Transition_FailedThenRetry_PersistsFailedFromStateAndResetsIt()
    {
        var created = await _factory.CreateExecutionAsync();
        var coding = await _factory.AdvanceAsync(
            created,
            ("RequirementAnalysis", "N8n"),
            ("AwaitingRequirementApproval", "Agent"),
            ("ImplementationPlanning", "Human"),
            ("Coding", "Agent"));

        var failed = await _factory.AdvanceAsync(coding, ("Failed", "System"));
        Assert.Equal("Coding", (await _factory.GetAsync(failed.Id)).FailedFromState);

        var retried = await _factory.AdvanceAsync(failed, ("Coding", "Human"));

        var fetched = await _factory.GetAsync(retried.Id);
        Assert.Equal("Coding", fetched.CurrentState);
        Assert.Null(fetched.FailedFromState);
    }

    // ---- Đầu vào sai ----

    [Theory]
    [InlineData("")]
    [InlineData("Archived")]
    [InlineData("1")]
    [InlineData("99")]
    public async Task Transition_InvalidStateName_Returns400(string toState)
    {
        var created = await _factory.CreateExecutionAsync();

        var res = await _factory.TransitionAsync(created.Id, toState, "N8n", created.Version);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Transition_InvalidActorType_Returns400()
    {
        var created = await _factory.CreateExecutionAsync();

        var res = await _factory.TransitionAsync(created.Id, "RequirementAnalysis", "Robot", created.Version);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Transition_MissingExpectedVersion_Returns400()
    {
        var created = await _factory.CreateExecutionAsync();

        var res = await _factory.CreateClient().PostAsJsonAsync($"/executions/{created.Id}/transitions", new
        {
            toState = "RequirementAnalysis",
            actor = new { type = "N8n", id = "n8n" }
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetProperty("errors").TryGetProperty("expectedVersion", out _));
    }

    [Fact]
    public async Task Transition_UnknownExecution_Returns404()
    {
        var res = await _factory.TransitionAsync(Guid.NewGuid(), "RequirementAnalysis", "N8n", 1);

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
