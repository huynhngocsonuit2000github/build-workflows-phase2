using Microsoft.EntityFrameworkCore;
using TaskLite.Api.Auth;
using TaskLite.Api.Data;
using TaskLite.Api.Domain;

namespace TaskLite.Api.Endpoints;

public static class TaskEndpoints
{
    public static RouteGroupBuilder MapTaskEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/projects/{projectId:int}/tasks")
            .AddEndpointFilter<RequireUserFilter>();

        group.MapPost("/", CreateTask);
        group.MapGet("/", GetTasks);
        group.MapGet("/{taskId:int}", GetTaskById);
        group.MapPatch("/{taskId:int}/status", UpdateTaskStatus);

        return group;
    }

    private static async Task<(Project? project, IResult? errorResult)> ValidateProjectMembership(
        int projectId,
        CurrentUser user,
        TaskLiteDbContext db)
    {
        var project = await db.Projects.FindAsync(projectId);
        if (project == null)
        {
            return (null, Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: $"Project {projectId} not found."));
        }

        if (!project.MemberIds.Contains(user.Id, StringComparer.OrdinalIgnoreCase))
        {
            return (null, Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: "User is not a member of this project."));
        }

        return (project, null);
    }

    private static async Task<IResult> CreateTask(
        int projectId,
        CreateTaskRequest request,
        CurrentUser user,
        TaskLiteDbContext db)
    {
        var (project, errorResult) = await ValidateProjectMembership(projectId, user, db);
        if (errorResult != null)
        {
            return errorResult;
        }

        var title = request.Title?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(title) || title.Length > 200)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["title"] = ["Title is required and must be between 1 and 200 characters."]
            });
        }

        string? assignee = string.IsNullOrWhiteSpace(request.Assignee) ? null : request.Assignee.Trim();
        if (assignee != null && !project!.MemberIds.Contains(assignee, StringComparer.OrdinalIgnoreCase))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["assignee"] = ["Assignee must be a member of the project."]
            });
        }

        var task = new TaskItem
        {
            ProjectId = projectId,
            Title = title,
            Status = TaskItemStatus.Todo,
            Assignee = assignee,
            DueDate = request.DueDate,
            CreatedAt = DateTime.UtcNow
        };

        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        var response = new TaskResponse(
            task.Id,
            task.ProjectId,
            task.Title,
            task.Status,
            task.Assignee,
            task.DueDate,
            task.CreatedAt);

        return Results.Created($"/projects/{projectId}/tasks/{task.Id}", response);
    }

    private static async Task<IResult> GetTasks(
        int projectId,
        CurrentUser user,
        TaskLiteDbContext db)
    {
        var (_, errorResult) = await ValidateProjectMembership(projectId, user, db);
        if (errorResult != null)
        {
            return errorResult;
        }

        var tasks = await db.Tasks
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.Id)
            .Select(t => new TaskResponse(
                t.Id,
                t.ProjectId,
                t.Title,
                t.Status,
                t.Assignee,
                t.DueDate,
                t.CreatedAt))
            .ToListAsync();

        return Results.Ok(tasks);
    }

    private static async Task<IResult> GetTaskById(
        int projectId,
        int taskId,
        CurrentUser user,
        TaskLiteDbContext db)
    {
        var (_, errorResult) = await ValidateProjectMembership(projectId, user, db);
        if (errorResult != null)
        {
            return errorResult;
        }

        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId);
        if (task == null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: $"Task {taskId} not found in project {projectId}.");
        }

        var response = new TaskResponse(
            task.Id,
            task.ProjectId,
            task.Title,
            task.Status,
            task.Assignee,
            task.DueDate,
            task.CreatedAt);

        return Results.Ok(response);
    }

    private static async Task<IResult> UpdateTaskStatus(
        int projectId,
        int taskId,
        UpdateTaskStatusRequest request,
        CurrentUser user,
        TaskLiteDbContext db)
    {
        var (_, errorResult) = await ValidateProjectMembership(projectId, user, db);
        if (errorResult != null)
        {
            return errorResult;
        }

        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId);
        if (task == null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: $"Task {taskId} not found in project {projectId}.");
        }

        if (string.IsNullOrWhiteSpace(request.Status) ||
            !Enum.TryParse<TaskItemStatus>(request.Status.Trim(), ignoreCase: true, out var newStatus) ||
            !Enum.IsDefined(newStatus))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["status"] = ["Invalid status. Allowed values are: Todo, InProgress, Done."]
            });
        }

        task.Status = newStatus;
        await db.SaveChangesAsync();

        var response = new TaskResponse(
            task.Id,
            task.ProjectId,
            task.Title,
            task.Status,
            task.Assignee,
            task.DueDate,
            task.CreatedAt);

        return Results.Ok(response);
    }
}
