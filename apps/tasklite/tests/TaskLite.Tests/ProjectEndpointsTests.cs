using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TaskLite.Api.Endpoints;

namespace TaskLite.Tests;

public class ProjectEndpointsTests : IDisposable
{
    private readonly TaskLiteFactory _factory = new();

    [Fact]
    public async Task CreateProject_ValidRequest_Returns201WithLocationAndCreatorInMembers()
    {
        var (res, proj) = await _factory.CreateProjectAsync("alice", "Project Alpha", ["bob"]);

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        Assert.NotNull(res.Headers.Location);
        Assert.NotNull(proj);
        Assert.Equal("Project Alpha", proj.Name);
        Assert.Contains("alice", proj.MemberIds);
        Assert.Contains("bob", proj.MemberIds);
        Assert.EndsWith($"/projects/{proj.Id}", res.Headers.Location.ToString());
    }

    [Fact]
    public async Task GetProjects_MultipleUsers_ReturnsOnlyProjectsUserIsMemberOf()
    {
        await _factory.CreateProjectAsync("alice", "Alice's Private Project");
        await _factory.CreateProjectAsync("bob", "Bob's Project", ["alice"]);
        await _factory.CreateProjectAsync("charlie", "Charlie's Project");

        var client = _factory.CreateClientForUser("alice");
        var res = await client.GetAsync("/projects");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var projects = await res.Content.ReadFromJsonAsync<List<ProjectResponse>>(TaskLiteFactory.JsonOptions);
        Assert.NotNull(projects);
        Assert.Equal(2, projects.Count);
        Assert.Contains(projects, p => p.Name == "Alice's Private Project");
        Assert.Contains(projects, p => p.Name == "Bob's Project");
        Assert.DoesNotContain(projects, p => p.Name == "Charlie's Project");
    }

    [Fact]
    public async Task GetProjectById_MemberUser_Returns200WithProjectDetails()
    {
        var (_, created) = await _factory.CreateProjectAsync("alice", "Secret Alpha");

        var client = _factory.CreateClientForUser("alice");
        var res = await client.GetAsync($"/projects/{created!.Id}");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var proj = await res.Content.ReadFromJsonAsync<ProjectResponse>(TaskLiteFactory.JsonOptions);
        Assert.NotNull(proj);
        Assert.Equal("Secret Alpha", proj.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateProject_EmptyOrWhitespaceName_Returns400WithErrorsName(string name)
    {
        var client = _factory.CreateClientForUser("alice");
        var res = await client.PostAsJsonAsync("/projects", new CreateProjectRequest(name, null));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var json = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var errors = doc.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty("name", out _) || errors.TryGetProperty("Name", out _));
    }

    [Fact]
    public async Task CreateProject_MalformedJson_Returns400()
    {
        var client = _factory.CreateClientForUser("alice");
        var content = new StringContent("{ invalid json content }", Encoding.UTF8, "application/json");
        var res = await client.PostAsync("/projects", content);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task GetProjectById_NonExistentProject_Returns404ProblemDetails()
    {
        var client = _factory.CreateClientForUser("alice");
        var res = await client.GetAsync("/projects/99999");

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        Assert.Equal("application/problem+json", res.Content.Headers.ContentType?.MediaType);
        var json = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(404, doc.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Not Found", doc.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetProjects_MissingUserIdHeader_Returns401ProblemDetails()
    {
        var client = _factory.CreateClient(); // no X-User-Id
        var res = await client.GetAsync("/projects");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        Assert.Equal("application/problem+json", res.Content.Headers.ContentType?.MediaType);
        var json = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(401, doc.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Unauthorized", doc.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetProjects_WhitespaceUserIdHeader_Returns401ProblemDetails()
    {
        var client = _factory.CreateClientForUser("   ");
        var res = await client.GetAsync("/projects");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        var json = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(401, doc.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task GetProjectById_NonMemberUser_Returns403ProblemDetails()
    {
        var (_, created) = await _factory.CreateProjectAsync("alice", "Alice Private");

        var malloryClient = _factory.CreateClientForUser("mallory");
        var res = await malloryClient.GetAsync($"/projects/{created!.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        Assert.Equal("application/problem+json", res.Content.Headers.ContentType?.MediaType);
        var json = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(403, doc.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Forbidden", doc.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetProjectById_DifferentCasedUserId_Returns403_Regression01()
    {
        // 0.1 Hồi quy: ALICE đọc project của alice phải trả 403 Forbidden
        var (_, created) = await _factory.CreateProjectAsync("alice", "Case Sensitive Project");

        var upperClient = _factory.CreateClientForUser("ALICE");
        var res = await upperClient.GetAsync($"/projects/{created!.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task GetProjects_DifferentCasedUserId_ReturnsEmptyList_Regression01()
    {
        // 0.1 Hồi quy: ALICE liệt kê project của alice -> danh sách rỗng
        await _factory.CreateProjectAsync("alice", "Case Sensitive Project");

        var upperClient = _factory.CreateClientForUser("ALICE");
        var res = await upperClient.GetAsync("/projects");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var projects = await res.Content.ReadFromJsonAsync<List<ProjectResponse>>(TaskLiteFactory.JsonOptions);
        Assert.NotNull(projects);
        Assert.Empty(projects);
    }

    [Fact]
    public async Task CreateProject_DuplicateMemberIds_PersistsDistinctMembers()
    {
        var (_, created) = await _factory.CreateProjectAsync("alice", "Distinct Members", ["bob", "bob", "alice"]);

        Assert.NotNull(created);
        Assert.Equal(2, created.MemberIds.Count);
        Assert.Contains("alice", created.MemberIds);
        Assert.Contains("bob", created.MemberIds);
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}
