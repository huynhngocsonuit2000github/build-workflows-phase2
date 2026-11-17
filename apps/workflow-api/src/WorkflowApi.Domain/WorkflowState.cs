namespace WorkflowApi.Domain;

/// <summary>13 trạng thái của một lần chạy (xem docs/architecture/workflow-states.md).</summary>
public enum WorkflowState
{
    Created,
    RequirementAnalysis,
    AwaitingRequirementApproval,
    ImplementationPlanning,
    Coding,
    BuildAndTest,
    CodeReview,
    AwaitingCodeApproval,
    PullRequestCreated,
    ReadyForDeployment,
    Completed,
    Failed,
    Cancelled
}

public static class WorkflowStateExtensions
{
    /// <summary>Trạng thái cuối: không đi tiếp được. Failed không phải trạng thái cuối vì còn retry.</summary>
    public static bool IsTerminal(this WorkflowState state) =>
        state is WorkflowState.Completed or WorkflowState.Cancelled;
}
