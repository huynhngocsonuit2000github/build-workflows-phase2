using WorkflowApi.Domain;

namespace WorkflowApi.Api.Endpoints;

public record ActorDto(string? Type, string? Id);

/// <summary>Đi kèm header bắt buộc Idempotency-Key (xem ExecutionEndpoints.CreateExecution).</summary>
public record CreateExecutionRequest(string? JiraIssueKey, string? CorrelationId, ActorDto? Actor);

public record TransitionRequestDto(string? ToState, ActorDto? Actor, long? ExpectedVersion, string? Reason);

public record ExecutionResponse(
    Guid Id,
    string JiraIssueKey,
    string CorrelationId,
    string CurrentState,
    long Version,
    string? FailedFromState,
    int RequirementRounds,
    int FixAttempts,
    int ReviewRounds,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static ExecutionResponse From(WorkflowExecution e) => new(
        e.Id,
        e.JiraIssueKey,
        e.CorrelationId,
        e.CurrentState.ToString(),
        e.Version,
        e.FailedFromState?.ToString(),
        e.RequirementRounds,
        e.FixAttempts,
        e.ReviewRounds,
        e.CreatedAt,
        e.UpdatedAt);
}

public record HistoryResponse(
    string? FromState,
    string ToState,
    DateTime ChangedAt,
    string ChangedBy,
    string? Reason)
{
    public static HistoryResponse From(WorkflowHistory h) => new(
        h.FromState?.ToString(),
        h.ToState.ToString(),
        h.ChangedAt,
        h.ChangedBy,
        h.Reason);
}
