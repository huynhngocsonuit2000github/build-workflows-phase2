using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TaskLite.Api.Domain;
using TaskLite.Api.Endpoints;

namespace TaskLite.Tests;

public class TaskEndpointsTests : IDisposable
{
    private readonly TaskLiteFactory _factory = new();

    [Fact]
    public async Task CreateTask_ValidRequest_Returns201WithLocationAndTodoStatus()
    {
        var (_, proj) = await _factory.CreateProjectAsync("alice", "Project Alpha", ["bob"]);
        var (res, task) = await _factory.CreateTaskAsync("alice", proj!.Id, "Initial setup task", "bob", new DateOnly(2026, 12, 1));

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        Assert.NotNull(res.Headers.Location);
        Assert.NotNull(task);
        Assert.Equal("Initial setup task", task.Title);
        Assert.Equal(TaskItemStatus.Todo, task.Status);
        Assert.Equal("bob", task.Assignee);
        Assert.Equal(new DateOnly(2026, 12, 1), task.DueDate);
        Assert.EndsWith($"/projects/{proj.Id}/tasks/{task.Id}", res.Headers.Location.ToString());
    }

    [Fact]
    public async Task GetTaskById_ExistingTask_Returns200WithTask()
    {
        var (_, proj) = await _factory.CreateProjectAsync("alice", "Project Alpha");
        var (_, task) = await _factory.CreateTaskAsync("alice", proj!.Id, "Task 1");

        var client = _factory.CreateClientForUser("alice");
        var res = await client.GetAsync($"/projects/{proj.Id}/tasks/{task!.Id}");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var fetched = await res.Content.ReadFromJsonAsync<TaskResponse>(TaskLiteFactory.JsonOptions);
        Assert.NotNull(fetched);
        Assert.Equal(task.Id, fetched.Id);
        Assert.Equal("Task 1", fetched.Title);
    }

    [Fact]
    public async Task GetTasks_MultipleTasks_ReturnsTasksSortedByIdAscending()
    {
        var (_, proj) = await _factory.CreateProjectAsync("alice", "Project Alpha");
        var (_, t1) = await _factory.CreateTaskAsync("alice", proj!.Id, "First Task");
        var (_, t2) = await _factory.CreateTaskAsync("alice", proj!.Id, "Second Task");
        var (_, t3) = await _factory.CreateTaskAsync("alice", proj!.Id, "Third Task");

        var client = _factory.CreateClientForUser("alice");
        var res = await client.GetAsync($"/projects/{proj.Id}/tasks");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var tasks = await res.Content.ReadFromJsonAsync<List<TaskResponse>>(TaskLiteFactory.JsonOptions);
        Assert.NotNull(tasks);
        Assert.Equal(3, tasks.Count);
        Assert.True(tasks[0].Id < tasks[1].Id);
        Assert.True(tasks[1].Id < tasks[2].Id);
    }

    [Fact]
    public async Task UpdateTaskStatus_ValidTransitions_Returns200AndUpdatesStatus()
    {
        var (_, proj) = await _factory.CreateProjectAsync("alice", "Project Alpha");
        var (_, task) = await _factory.CreateTaskAsync("alice", proj!.Id, "Task to update");

        var client = _factory.CreateClientForUser("alice");

        // Transition to InProgress
        var resProgress = await client.PatchAsJsonAsync(
            $"/projects/{proj.Id}/tasks/{task!.Id}/status",
            new UpdateTaskStatusRequest("InProgress"));
        Assert.Equal(HttpStatusCode.OK, resProgress.StatusCode);
        var taskProgress = await resProgress.Content.ReadFromJsonAsync<TaskResponse>(TaskLiteFactory.JsonOptions);
        Assert.NotNull(taskProgress);
        Assert.Equal(TaskItemStatus.InProgress, taskProgress.Status);

        // Transition to Done
        var resDone = await client.PatchAsJsonAsync(
            $"/projects/{proj.Id}/tasks/{task.Id}/status",
            new UpdateTaskStatusRequest("Done"));
        Assert.Equal(HttpStatusCode.OK, resDone.StatusCode);
        var taskDone = await resDone.Content.ReadFromJsonAsync<TaskResponse>(TaskLiteFactory.JsonOptions);
        Assert.NotNull(taskDone);
        Assert.Equal(TaskItemStatus.Done, taskDone.Status);

        // Verify read back
        var resFetch = await client.GetAsync($"/projects/{proj.Id}/tasks/{task.Id}");
        var fetched = await resFetch.Content.ReadFromJsonAsync<TaskResponse>(TaskLiteFactory.JsonOptions);
        Assert.NotNull(fetched);
        Assert.Equal(TaskItemStatus.Done, fetched.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateTask_EmptyOrWhitespaceTitle_Returns400WithErrorsTitle(string title)
    {
        var (_, proj) = await _factory.CreateProjectAsync("alice", "Project Alpha");
        var client = _factory.CreateClientForUser("alice");

        var res = await client.PostAsJsonAsync($"/projects/{proj!.Id}/tasks", new CreateTaskRequest(title, null, null));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

        var json = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var errors = doc.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty("title", out _) || errors.TryGetProperty("Title", out _));
    }

    [Fact]
    public async Task CreateTask_TitleOver200Chars_Returns400WithErrorsTitle()
    {
        var (_, proj) = await _factory.CreateProjectAsync("alice", "Project Alpha");
        var client = _factory.CreateClientForUser("alice");

        var longTitle = new string('A', 201);
        var res = await client.PostAsJsonAsync($"/projects/{proj!.Id}/tasks", new CreateTaskRequest(longTitle, null, null));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

        var json = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var errors = doc.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty("title", out _) || errors.TryGetProperty("Title", out _));
    }

    [Fact]
    public async Task CreateTask_AssigneeNotMember_Returns400()
    {
        var (_, proj) = await _factory.CreateProjectAsync("alice", "Project Alpha", ["bob"]);
        var client = _factory.CreateClientForUser("alice");

        var res = await client.PostAsJsonAsync(
            $"/projects/{proj!.Id}/tasks",
            new CreateTaskRequest("Valid Title", "mallory", null));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task UpdateTaskStatus_ArchivedStatus_Returns400()
    {
        var (_, proj) = await _factory.CreateProjectAsync("alice", "Project Alpha");
        var (_, task) = await _factory.CreateTaskAsync("alice", proj!.Id, "Task");

        var client = _factory.CreateClientForUser("alice");
        var res = await client.PatchAsJsonAsync(
            $"/projects/{proj.Id}/tasks/{task!.Id}/status",
            new UpdateTaskStatusRequest("Archived"));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("0")]
    [InlineData("99")]
    public async Task UpdateTaskStatus_NumericStatus_Returns400_Regression02(string numericStatus)
    {
        // 0.2 Hồi quy: Status dạng số phải bị từ chối với 400 có errors.status
        var (_, proj) = await _factory.CreateProjectAsync("alice", "Project Alpha");
        var (_, task) = await _factory.CreateTaskAsync("alice", proj!.Id, "Task");

        var client = _factory.CreateClientForUser("alice");
        var res = await client.PatchAsJsonAsync(
            $"/projects/{proj.Id}/tasks/{task!.Id}/status",
            new UpdateTaskStatusRequest(numericStatus));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var json = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var errors = doc.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty("status", out _) || errors.TryGetProperty("Status", out _));
    }

    [Fact]
    public async Task CreateTask_MalformedJson_Returns400()
    {
        var (_, proj) = await _factory.CreateProjectAsync("alice", "Project Alpha");
        var client = _factory.CreateClientForUser("alice");

        var content = new StringContent("{ broken json", Encoding.UTF8, "application/json");
        var res = await client.PostAsync($"/projects/{proj!.Id}/tasks", content);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task CreateTask_NonExistentProject_Returns404()
    {
        var client = _factory.CreateClientForUser("alice");
        var res = await client.PostAsJsonAsync("/projects/99999/tasks", new CreateTaskRequest("Title", null, null));

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task GetTaskById_TaskBelongsToAnotherProject_Returns404()
    {
        var (_, p1) = await _factory.CreateProjectAsync("alice", "Project 1");
        var (_, p2) = await _factory.CreateProjectAsync("alice", "Project 2");
        var (_, taskInP2) = await _factory.CreateTaskAsync("alice", p2!.Id, "Task in P2");

        var client = _factory.CreateClientForUser("alice");
        var res = await client.GetAsync($"/projects/{p1!.Id}/tasks/{taskInP2!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task GetTasks_NonMemberUser_Returns403()
    {
        var (_, proj) = await _factory.CreateProjectAsync("alice", "Alice's Project");
        var mallory = _factory.CreateClientForUser("mallory");

        var res = await mallory.GetAsync($"/projects/{proj!.Id}/tasks");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task CreateTask_NonMemberUser_Returns403()
    {
        var (_, proj) = await _factory.CreateProjectAsync("alice", "Alice's Project");
        var mallory = _factory.CreateClientForUser("mallory");

        var res = await mallory.PostAsJsonAsync($"/projects/{proj!.Id}/tasks", new CreateTaskRequest("New Task", null, null));
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task UpdateTaskStatus_NonMemberUser_Returns403()
    {
        var (_, proj) = await _factory.CreateProjectAsync("alice", "Alice's Project");
        var (_, task) = await _factory.CreateTaskAsync("alice", proj!.Id, "Task");
        var mallory = _factory.CreateClientForUser("mallory");

        var res = await mallory.PatchAsJsonAsync(
            $"/projects/{proj.Id}/tasks/{task!.Id}/status",
            new UpdateTaskStatusRequest("Done"));
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task CreateTask_SameTitleTwice_Returns201ForBothWithDifferentIds()
    {
        var (_, proj) = await _factory.CreateProjectAsync("alice", "Project Alpha");

        var (r1, t1) = await _factory.CreateTaskAsync("alice", proj!.Id, "Duplicate Title");
        var (r2, t2) = await _factory.CreateTaskAsync("alice", proj.Id, "Duplicate Title");

        Assert.Equal(HttpStatusCode.Created, r1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, r2.StatusCode);
        Assert.NotNull(t1);
        Assert.NotNull(t2);
        Assert.NotEqual(t1.Id, t2.Id);
    }

    [Fact]
    public async Task CreateTask_CreatedAtField_MatchesOnReadAndEndsWithZ_Regression03()
    {
        // 0.3 Hồi quy: createdAt của task lúc tạo và lúc đọc lại bằng nhau và kết thúc bằng 'Z'
        var (_, proj) = await _factory.CreateProjectAsync("alice", "Project Alpha");
        var client = _factory.CreateClientForUser("alice");

        var postRes = await client.PostAsJsonAsync(
            $"/projects/{proj!.Id}/tasks",
            new CreateTaskRequest("Check UTC timestamp", null, null));
        Assert.Equal(HttpStatusCode.Created, postRes.StatusCode);

        var createdJson = await postRes.Content.ReadAsStringAsync();
        using var createdDoc = JsonDocument.Parse(createdJson);
        var createdTimeStr = createdDoc.RootElement.GetProperty("createdAt").GetString();
        Assert.NotNull(createdTimeStr);
        Assert.EndsWith("Z", createdTimeStr);

        var taskId = createdDoc.RootElement.GetProperty("id").GetInt32();

        // Đọc lại từ database
        var getRes = await client.GetAsync($"/projects/{proj.Id}/tasks/{taskId}");
        Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);

        var readJson = await getRes.Content.ReadAsStringAsync();
        using var readDoc = JsonDocument.Parse(readJson);
        var readTimeStr = readDoc.RootElement.GetProperty("createdAt").GetString();
        Assert.NotNull(readTimeStr);
        Assert.EndsWith("Z", readTimeStr);

        // Khớp chính xác chuỗi createdAt lúc tạo và lúc đọc lại
        Assert.Equal(createdTimeStr, readTimeStr);
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}
