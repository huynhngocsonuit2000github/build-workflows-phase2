using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskLite.Api.Data;
using TaskLite.Api.Endpoints;

namespace TaskLite.Tests;

public class TaskLiteFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"tasklite-test-{Guid.NewGuid():N}.db");

    public string DbPath => _dbPath;

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_dbPath}");
        builder.UseSetting("Seed:DemoData", "false");

        builder.ConfigureTestServices(services =>
        {
            var descriptors = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<TaskLiteDbContext>) ||
                d.ServiceType == typeof(TaskLiteDbContext)).ToList();

            foreach (var d in descriptors)
            {
                services.Remove(d);
            }

            services.AddDbContext<TaskLiteDbContext>(options =>
                options.UseSqlite($"Data Source={_dbPath}"));
        });
    }

    public HttpClient CreateClientForUser(string? userId = null)
    {
        var client = CreateClient();
        if (userId != null)
        {
            client.DefaultRequestHeaders.Add("X-User-Id", userId);
        }
        return client;
    }

    public async Task<(HttpResponseMessage Response, ProjectResponse? Project)> CreateProjectAsync(
        string user, string name, List<string>? members = null)
    {
        var client = CreateClientForUser(user);
        var res = await client.PostAsJsonAsync("/projects", new CreateProjectRequest(name, members));
        var proj = res.IsSuccessStatusCode ? await res.Content.ReadFromJsonAsync<ProjectResponse>(JsonOptions) : null;
        return (res, proj);
    }

    public async Task<(HttpResponseMessage Response, TaskResponse? Task)> CreateTaskAsync(
        string user, int projectId, string title, string? assignee = null, DateOnly? dueDate = null)
    {
        var client = CreateClientForUser(user);
        var res = await client.PostAsJsonAsync($"/projects/{projectId}/tasks", new CreateTaskRequest(title, assignee, dueDate));
        var task = res.IsSuccessStatusCode ? await res.Content.ReadFromJsonAsync<TaskResponse>(JsonOptions) : null;
        return (res, task);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            try
            {
                SqliteConnection.ClearAllPools();
                if (File.Exists(_dbPath))
                {
                    File.Delete(_dbPath);
                }
                var shm = $"{_dbPath}-shm";
                if (File.Exists(shm)) File.Delete(shm);
                var wal = $"{_dbPath}-wal";
                if (File.Exists(wal)) File.Delete(wal);
            }
            catch
            {
                // Ignore cleanup errors during disposal
            }
        }
    }
}
