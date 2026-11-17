using WorkflowApi.Domain;

namespace WorkflowApi.Domain.Tests;

internal static class TestHelpers
{
    public static readonly DateTime Now = new(2026, 11, 16, 9, 0, 0, DateTimeKind.Utc);

    public static Actor N8n => new(ActorType.N8n, "n8n");
    public static Actor Agent => new(ActorType.Agent, "agent-1");
    public static Actor Human => new(ActorType.Human, "son");
    public static Actor Ci => new(ActorType.Ci, "github-actions");
    public static Actor Sys => new(ActorType.System, "workflow-api");

    /// <summary>Đường chính, theo thứ tự, kèm actor đúng cho từng bước.</summary>
    public static readonly (WorkflowState To, Actor Actor)[] HappyPath =
    [
        (WorkflowState.RequirementAnalysis, N8n),
        (WorkflowState.AwaitingRequirementApproval, Agent),
        (WorkflowState.ImplementationPlanning, Human),
        (WorkflowState.Coding, Agent),
        (WorkflowState.BuildAndTest, Agent),
        (WorkflowState.CodeReview, Ci),
        (WorkflowState.AwaitingCodeApproval, Agent),
        (WorkflowState.PullRequestCreated, Human),
        (WorkflowState.ReadyForDeployment, Ci),
        (WorkflowState.Completed, Ci)
    ];

    public static WorkflowExecution NewExecution() =>
        WorkflowExecution.Create("TAS-5", "corr-1", N8n, Now);

    /// <summary>Tạo execution rồi đi theo đường chính đến đúng trạng thái yêu cầu.</summary>
    public static WorkflowExecution At(WorkflowState target)
    {
        var execution = NewExecution();
        if (target == WorkflowState.Created)
        {
            return execution;
        }

        foreach (var (to, actor) in HappyPath)
        {
            Step(execution, to, actor);
            if (to == target)
            {
                return execution;
            }
        }

        throw new ArgumentException($"{target} is not on the happy path.", nameof(target));
    }

    /// <summary>Chuyển và khẳng định thành công; dùng để dựng tình huống.</summary>
    public static void Step(WorkflowExecution execution, WorkflowState to, Actor actor)
    {
        var result = Try(execution, to, actor);
        Assert.True(result.Succeeded, $"Setup step {execution.CurrentState} -> {to} failed: {result.Message}");
    }

    public static TransitionResult Try(WorkflowExecution execution, WorkflowState to, Actor actor, string? reason = null) =>
        WorkflowStateMachine.Apply(execution, new TransitionRequest(to, actor, execution.Version, reason), Now);

    public static IEnumerable<object[]> NonTerminalStates() =>
        Enum.GetValues<WorkflowState>().Where(s => !s.IsTerminal()).Select(s => new object[] { s });

    public static IEnumerable<object[]> AllStates() =>
        Enum.GetValues<WorkflowState>().Select(s => new object[] { s });
}
