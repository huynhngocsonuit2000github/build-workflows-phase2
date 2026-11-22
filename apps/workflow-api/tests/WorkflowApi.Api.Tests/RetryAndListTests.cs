using System.Net;
using System.Net.Http.Json;
using WorkflowApi.Api.Endpoints;

namespace WorkflowApi.Api.Tests;

/// <summary>GET /executions?state=... và POST /executions/{id}/retry (s1-5).</summary>
[Collection("postgres")]
public class RetryAndListTests(PostgresFixture postgres) : IAsyncLifetime
{
    private WorkflowApiFactory _factory = null!;

    public async Task InitializeAsync() => _factory = await WorkflowApiFactory.CreateAsync(postgres);

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    // ---- Liệt kê ----

    [Fact]
    public async Task List_StateFailed_ReturnsOnlyFailedRunsWithFailedFromState()
    {
        var failed = await _factory.CreateFailedAtCodingAsync("TAS-1");
        await _factory.CreateExecutionAsync("TAS-2");

        var rows = await _factory.ListAsync("state=Failed");

        var row = Assert.Single(rows);
        Assert.Equal(failed.Id, row.Id);
        Assert.Equal("Failed", row.CurrentState);
        Assert.Equal("Coding", row.FailedFromState);
        Assert.Equal(failed.Version, row.Version);
    }

    [Fact]
    public async Task List_StateNameCaseInsensitive_Works()
    {
        await _factory.CreateFailedAtCodingAsync();

        Assert.Single(await _factory.ListAsync("state=failed"));
    }

    [Fact]
    public async Task List_NoFilter_ReturnsAllNewestUpdatedFirst()
    {
        var first = await _factory.CreateExecutionAsync("TAS-1");
        var second = await _factory.CreateExecutionAsync("TAS-2");
        await _factory.AdvanceAsync(first, ("RequirementAnalysis", "N8n")); // first cập nhật sau cùng

        var rows = await _factory.ListAsync("");

        Assert.Equal([first.Id, second.Id], rows.Select(r => r.Id));
    }

    [Fact]
    public async Task List_ByIssueAndState_Combines()
    {
        await _factory.CreateFailedAtCodingAsync("TAS-1");
        await _factory.CreateFailedAtCodingAsync("TAS-2");

        var rows = await _factory.ListAsync("state=Failed&jiraIssueKey=TAS-2");

        Assert.Equal("TAS-2", Assert.Single(rows).JiraIssueKey);
    }

    [Fact]
    public async Task List_Limit_CapsRows()
    {
        for (var i = 0; i < 3; i++)
        {
            await _factory.CreateExecutionAsync($"TAS-{i}");
        }

        Assert.Equal(2, (await _factory.ListAsync("limit=2")).Count);
    }

    [Theory]
    [InlineData("state=Archived")]
    [InlineData("state=11")]
    [InlineData("state=")]
    [InlineData("limit=0")]
    [InlineData("limit=201")]
    public async Task List_InvalidQuery_Returns400(string query)
    {
        var res = await _factory.CreateClient().GetAsync($"/executions?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    // ---- Retry ----

    [Fact]
    public async Task Retry_Failed_Returns200BackToFailedFromStateSameId()
    {
        var failed = await _factory.CreateFailedAtCodingAsync();

        var res = await _factory.RetryAsync(failed.Id, "Human", failed.Version, "provider is back");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = (await res.Content.ReadFromJsonAsync<ExecutionResponse>())!;
        Assert.Equal(failed.Id, body.Id);
        Assert.Equal("Coding", body.CurrentState);
        Assert.Null(body.FailedFromState);
        Assert.Equal(failed.Version + 1, body.Version);
    }

    [Fact]
    public async Task Retry_Failed_WritesHistoryRowWithActorAndReason()
    {
        var failed = await _factory.CreateFailedAtCodingAsync();

        await _factory.RetryAsync(failed.Id, "Human", failed.Version, "provider is back", actorId: "son");

        var last = (await _factory.GetHistoryAsync(failed.Id))[^1];
        Assert.Equal("Failed", last.FromState);
        Assert.Equal("Coding", last.ToState);
        Assert.Equal("Human:son", last.ChangedBy);
        Assert.Equal("provider is back", last.Reason);
    }

    [Fact]
    public async Task Retry_Failed_DoesNotCreateNewRun()
    {
        var failed = await _factory.CreateFailedAtCodingAsync("TAS-9");
        var before = await _factory.CountAllExecutionsAsync();

        await _factory.RetryAsync(failed.Id, "Human", failed.Version);

        Assert.Equal(before, await _factory.CountAllExecutionsAsync());
        Assert.Equal(1, await _factory.CountExecutionsAsync("TAS-9"));
    }

    [Fact]
    public async Task Retry_NotFailed_Returns422NotAllowedAndNothingChanges()
    {
        var created = await _factory.CreateExecutionAsync();

        var res = await _factory.RetryAsync(created.Id, "Human", created.Version);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, res.StatusCode);
        Assert.Equal("NotAllowed", await WorkflowApiFactory.ErrorCodeAsync(res));
        var after = await _factory.GetAsync(created.Id);
        Assert.Equal(("Created", created.Version), (after.CurrentState, after.Version));
        Assert.Single(await _factory.GetHistoryAsync(created.Id));
    }

    [Fact]
    public async Task Retry_ByAgent_Returns403()
    {
        var failed = await _factory.CreateFailedAtCodingAsync();

        var res = await _factory.RetryAsync(failed.Id, "Agent", failed.Version);

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        Assert.Equal("Failed", (await _factory.GetAsync(failed.Id)).CurrentState);
    }

    [Fact]
    public async Task Retry_StaleVersion_Returns409()
    {
        var failed = await _factory.CreateFailedAtCodingAsync();

        var res = await _factory.RetryAsync(failed.Id, "Human", failed.Version - 1);

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("VersionConflict", await WorkflowApiFactory.ErrorCodeAsync(res));
    }

    [Fact]
    public async Task Retry_SameRequestTwice_SecondIs409AndOnlyOneRetryInHistory()
    {
        var failed = await _factory.CreateFailedAtCodingAsync();

        var first = await _factory.RetryAsync(failed.Id, "Human", failed.Version);
        var second = await _factory.RetryAsync(failed.Id, "Human", failed.Version);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var history = await _factory.GetHistoryAsync(failed.Id);
        Assert.Single(history, h => h.FromState == "Failed");
    }

    /// <summary>Lặp nhiều vòng để các request thật sự chồng lên nhau (một vòng dễ chạy lần lượt, không chứng minh gì).</summary>
    [Fact]
    public async Task Retry_ParallelSameVersion_ExactlyOneSucceeds()
    {
        const int rounds = 10;
        const int parallelRequests = 8;

        for (var round = 0; round < rounds; round++)
        {
            var failed = await _factory.CreateFailedAtCodingAsync($"TAS-{round}");

            var responses = await Task.WhenAll(Enumerable.Range(0, parallelRequests)
                .Select(i => _factory.RetryAsync(failed.Id, "N8n", failed.Version, $"racer-{i}")));

            Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.OK));
            Assert.Equal(parallelRequests - 1, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
            Assert.Equal(failed.Version + 1, (await _factory.GetAsync(failed.Id)).Version);
            var history = await _factory.GetHistoryAsync(failed.Id);
            Assert.Single(history, h => h.FromState == "Failed");
        }
    }

    [Fact]
    public async Task Retry_AfterRetry_RunCanContinueOnHappyPath()
    {
        var failed = await _factory.CreateFailedAtCodingAsync();
        var res = await _factory.RetryAsync(failed.Id, "Human", failed.Version);
        var retried = (await res.Content.ReadFromJsonAsync<ExecutionResponse>())!;

        var next = await _factory.AdvanceAsync(retried, ("BuildAndTest", "Agent"));

        Assert.Equal("BuildAndTest", next.CurrentState);
    }

    [Fact]
    public async Task Retry_MissingExpectedVersion_Returns400()
    {
        var failed = await _factory.CreateFailedAtCodingAsync();

        var res = await _factory.CreateClient().PostAsJsonAsync(
            $"/executions/{failed.Id}/retry", new { actor = new { type = "Human", id = "son" } });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Retry_UnknownExecution_Returns404()
    {
        var res = await _factory.RetryAsync(Guid.NewGuid(), "Human", 1);

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
