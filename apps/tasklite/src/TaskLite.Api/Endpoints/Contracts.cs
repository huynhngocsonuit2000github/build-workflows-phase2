using TaskLite.Api.Domain;

namespace TaskLite.Api.Endpoints;

public record CreateProjectRequest(string? Name, List<string>? MemberIds);

public record ProjectResponse(int Id, string Name, List<string> MemberIds, DateTime CreatedAt);

public record CreateTaskRequest(string? Title, string? Assignee, DateOnly? DueDate);

public record UpdateTaskStatusRequest(string? Status);

public record TaskResponse(
    int Id,
    int ProjectId,
    string Title,
    TaskItemStatus Status,
    string? Assignee,
    DateOnly? DueDate,
    DateTime CreatedAt);
