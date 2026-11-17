using static WorkflowApi.Domain.WorkflowState;

namespace WorkflowApi.Domain;

/// <summary>
/// Bảng chuyển trạng thái hợp lệ (dữ liệu, không phải if/else rải rác).
/// Cặp (từ, sang) không có trong bảng nghĩa là bị từ chối.
/// Khớp với bảng trong docs/architecture/workflow-states.md.
/// </summary>
public static class TransitionTable
{
    public static IReadOnlyList<TransitionRule> Rules { get; } = Build();

    private static List<TransitionRule> Build()
    {
        var rules = new List<TransitionRule>();

        void Add(
            WorkflowState from,
            WorkflowState to,
            ActorType[] actors,
            Func<WorkflowExecution, bool>? guard = null,
            string? guardMessage = null,
            Action<WorkflowExecution>? effect = null) =>
            rules.Add(new TransitionRule(from, to, actors, guard, guardMessage, effect));

        // Đường chính
        Add(Created, RequirementAnalysis, [ActorType.N8n]);
        Add(RequirementAnalysis, AwaitingRequirementApproval, [ActorType.Agent]);
        Add(AwaitingRequirementApproval, ImplementationPlanning, [ActorType.Human]);
        Add(ImplementationPlanning, Coding, [ActorType.Agent]);
        Add(Coding, BuildAndTest, [ActorType.Agent]);
        Add(BuildAndTest, CodeReview, [ActorType.Ci]);
        Add(CodeReview, AwaitingCodeApproval, [ActorType.Agent]);
        Add(AwaitingCodeApproval, PullRequestCreated, [ActorType.Human]);
        Add(PullRequestCreated, ReadyForDeployment, [ActorType.Ci, ActorType.Human]);
        Add(ReadyForDeployment, Completed, [ActorType.Ci]);

        // Vòng lặp có giới hạn (số lần do code đếm, không phải LLM)
        Add(AwaitingRequirementApproval, RequirementAnalysis, [ActorType.Human],
            e => e.RequirementRounds < WorkflowLimits.MaxRequirementRounds,
            $"Requirement was already revised {WorkflowLimits.MaxRequirementRounds} times.",
            e => e.RequirementRounds++);

        Add(BuildAndTest, Coding, [ActorType.Ci],
            e => e.FixAttempts < WorkflowLimits.MaxFixAttempts,
            $"Already retried {WorkflowLimits.MaxFixAttempts} times after failing tests.",
            e => e.FixAttempts++);

        Add(BuildAndTest, Failed, [ActorType.Ci],
            e => e.FixAttempts >= WorkflowLimits.MaxFixAttempts,
            "Fix attempts are not exhausted yet.",
            e => e.FailedFromState = e.CurrentState);

        Add(CodeReview, Coding, [ActorType.Agent],
            e => e.ReviewRounds < WorkflowLimits.MaxReviewRounds,
            $"Already went through {WorkflowLimits.MaxReviewRounds} review rounds.",
            e => e.ReviewRounds++);

        Add(CodeReview, Failed, [ActorType.Agent], effect: e => e.FailedFromState = e.CurrentState);

        Add(AwaitingCodeApproval, Coding, [ActorType.Human]);

        // Mọi trạng thái chưa kết thúc đều có thể sang Failed (hệ thống) hoặc Cancelled (người)
        foreach (var state in Enum.GetValues<WorkflowState>().Where(s => !s.IsTerminal()))
        {
            if (state != Failed)
            {
                Add(state, Failed, [ActorType.System], effect: e => e.FailedFromState = e.CurrentState);
            }

            Add(state, Cancelled, [ActorType.Human]);
        }

        // Retry: chỉ từ Failed, quay về đúng trạng thái đã hỏng, đếm lại lượt
        foreach (var state in Enum.GetValues<WorkflowState>().Where(s => !s.IsTerminal() && s != Failed))
        {
            var target = state;
            Add(Failed, target, [ActorType.Human, ActorType.N8n],
                e => e.FailedFromState == target,
                $"Retry must go back to the state that failed, not {target}.",
                e =>
                {
                    e.FailedFromState = null;
                    e.RequirementRounds = 0;
                    e.FixAttempts = 0;
                    e.ReviewRounds = 0;
                });
        }

        return rules;
    }
}
