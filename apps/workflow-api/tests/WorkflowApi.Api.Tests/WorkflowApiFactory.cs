using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkflowApi.Api.Data;
using WorkflowApi.Api.Endpoints;

namespace WorkflowApi.Api.Tests;

/// <summary>Chạy API trong bộ nhớ, nối vào một database PostgreSQL riêng của test.</summary>
public sealed class WorkflowApiFactory : WebApplicationFactory<Program>
{
    private readonly PostgresFixture _postgres;
    private readonly string _databaseName;
    private readonly string _connectionString;

    private WorkflowApiFactory(PostgresFixture postgres, string databaseName, string connectionString)
    {
        _postgres = postgres;
        _databaseName = databaseName;
        _connectionString = connectionString;
    }

    public static async Task<WorkflowApiFactory> CreateAsync(PostgresFixture postgres)
    {
        var (name, connectionString) = await postgres.CreateDatabaseAsync();
        return new WorkflowApiFactory(postgres, name, connectionString);
    }

    public string ConnectionString => _connectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", _connectionString);
    }

    // ---- Helper gọi API ----

    public async Task<ExecutionResponse> CreateExecutionAsync(string key = "TAS-5")
    {
        var res = await PostExecutionAsync(new
        {
            jiraIssueKey = key,
            correlationId = "corr-test",
            actor = new { type = "N8n", id = "n8n" }
        }, idempotencyKey: $"test:{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<ExecutionResponse>())!;
    }

    /// <summary>POST /executions với header Idempotency-Key (null = không gửi header).</summary>
    public Task<HttpResponseMessage> PostExecutionAsync(object body, string? idempotencyKey)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, "/executions") { Content = JsonContent.Create(body) };
        if (idempotencyKey is not null)
        {
            message.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        return CreateClient().SendAsync(message);
    }

    public async Task<int> CountExecutionsAsync(string jiraIssueKey)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();
        return await db.Executions.CountAsync(e => e.JiraIssueKey == jiraIssueKey);
    }

    public Task<HttpResponseMessage> TransitionAsync(
        Guid id, string toState, string actorType, long expectedVersion, string? reason = null, string actorId = "tester") =>
        CreateClient().PostAsJsonAsync($"/executions/{id}/transitions", new
        {
            toState,
            actor = new { type = actorType, id = actorId },
            expectedVersion,
            reason
        });

    public Task<HttpResponseMessage> RetryAsync(
        Guid id, string actorType, long expectedVersion, string? reason = null, string actorId = "tester") =>
        CreateClient().PostAsJsonAsync($"/executions/{id}/retry", new
        {
            actor = new { type = actorType, id = actorId },
            expectedVersion,
            reason
        });

    /// <summary>GET /executions với query string thô, ví dụ "state=Failed".</summary>
    public async Task<List<ExecutionSummary>> ListAsync(string query)
    {
        var res = await CreateClient().GetAsync($"/executions?{query}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<List<ExecutionSummary>>())!;
    }

    public async Task<int> CountAllExecutionsAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();
        return await db.Executions.CountAsync();
    }

    /// <summary>Tạo lần chạy, đi đến Coding rồi cho hệ thống đánh Failed (FailedFromState = Coding).</summary>
    public async Task<ExecutionResponse> CreateFailedAtCodingAsync(string key = "TAS-5")
    {
        var created = await CreateExecutionAsync(key);
        return await AdvanceAsync(
            created,
            ("RequirementAnalysis", "N8n"),
            ("AwaitingRequirementApproval", "Agent"),
            ("ImplementationPlanning", "Human"),
            ("Coding", "Agent"),
            ("Failed", "System"));
    }

    public async Task<ExecutionResponse> GetAsync(Guid id)
    {
        var res = await CreateClient().GetAsync($"/executions/{id}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<ExecutionResponse>())!;
    }

    public async Task<List<HistoryResponse>> GetHistoryAsync(Guid id)
    {
        var res = await CreateClient().GetAsync($"/executions/{id}/history");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<List<HistoryResponse>>())!;
    }

    /// <summary>Đi theo đường chính đến trạng thái yêu cầu (bằng chính API).</summary>
    public async Task<ExecutionResponse> AdvanceAsync(ExecutionResponse execution, params (string To, string Actor)[] steps)
    {
        var current = execution;
        foreach (var (to, actor) in steps)
        {
            var res = await TransitionAsync(current.Id, to, actor, current.Version);
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            current = (await res.Content.ReadFromJsonAsync<ExecutionResponse>())!;
        }

        return current;
    }

    public static async Task<string?> ErrorCodeAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.TryGetProperty("error", out var error) ? error.GetString() : null;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _postgres.DropDatabaseAsync(_databaseName).GetAwaiter().GetResult();
        }
    }
}
