using WorkflowApi.Domain;
using static WorkflowApi.Domain.Tests.TestHelpers;

namespace WorkflowApi.Domain.Tests;

public class WorkflowStateMachineTests
{
    // ---- Tạo lần chạy ----

    [Fact]
    public void Create_ValidInput_StartsAtCreatedVersion1WithOneHistoryRow()
    {
        var e = NewExecution();

        Assert.Equal(WorkflowState.Created, e.CurrentState);
        Assert.Equal(1, e.Version);
        Assert.Equal("TAS-5", e.JiraIssueKey);
        Assert.Equal("corr-1", e.CorrelationId);
        var row = Assert.Single(e.History);
        Assert.Null(row.FromState);
        Assert.Equal(WorkflowState.Created, row.ToState);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankIssueKey_Throws(string key)
    {
        Assert.Throws<ArgumentException>(() => WorkflowExecution.Create(key, "corr", N8n, Now));
    }

    // ---- Đường chính ----

    [Fact]
    public void Apply_HappyPath_ReachesCompletedWithOneHistoryRowPerStep()
    {
        var e = NewExecution();

        foreach (var (to, actor) in HappyPath)
        {
            Step(e, to, actor);
        }

        Assert.Equal(WorkflowState.Completed, e.CurrentState);
        Assert.Equal(HappyPath.Length + 1, e.Version);
        Assert.Equal(HappyPath.Length + 1, e.History.Count);
        Assert.Equal(WorkflowState.RequirementAnalysis, e.History[1].ToState);
        Assert.Equal(WorkflowState.Created, e.History[1].FromState);
    }

    [Fact]
    public void Apply_Success_RecordsActorReasonAndTime()
    {
        var e = NewExecution();

        var result = Try(e, WorkflowState.RequirementAnalysis, N8n, "jira ready");

        Assert.True(result.Succeeded);
        var row = e.History[^1];
        Assert.Equal("N8n:n8n", row.ChangedBy);
        Assert.Equal("jira ready", row.Reason);
        Assert.Equal(Now, row.ChangedAt);
        Assert.Equal(Now, e.UpdatedAt);
    }

    // ---- Chuyển sai: không có trong bảng ----

    [Fact]
    public void Apply_SkipApprovalGate_RejectedAndNothingChanges()
    {
        var e = At(WorkflowState.RequirementAnalysis);
        var version = e.Version;
        var rows = e.History.Count;

        var result = Try(e, WorkflowState.Coding, Agent);

        Assert.False(result.Succeeded);
        Assert.Equal(TransitionError.NotAllowed, result.Error);
        Assert.Equal(WorkflowState.RequirementAnalysis, e.CurrentState);
        Assert.Equal(version, e.Version);
        Assert.Equal(rows, e.History.Count);
    }

    [Fact]
    public void Apply_SameStateTransition_NotAllowed()
    {
        var e = At(WorkflowState.Coding);

        var result = Try(e, WorkflowState.Coding, Agent);

        Assert.Equal(TransitionError.NotAllowed, result.Error);
    }

    [Fact]
    public void Apply_BackwardTransitionNotInTable_NotAllowed()
    {
        var e = At(WorkflowState.CodeReview);

        var result = Try(e, WorkflowState.ImplementationPlanning, Agent);

        Assert.Equal(TransitionError.NotAllowed, result.Error);
    }

    // ---- Sai actor ----

    [Theory]
    [InlineData(ActorType.Agent)]
    [InlineData(ActorType.N8n)]
    [InlineData(ActorType.Ci)]
    [InlineData(ActorType.System)]
    public void Apply_RequirementApproval_OnlyHumanAllowed(ActorType type)
    {
        var e = At(WorkflowState.AwaitingRequirementApproval);

        var result = Try(e, WorkflowState.ImplementationPlanning, new Actor(type, "x"));

        Assert.Equal(TransitionError.ActorNotAllowed, result.Error);
        Assert.Equal(WorkflowState.AwaitingRequirementApproval, e.CurrentState);
    }

    [Fact]
    public void Apply_RequirementApproval_HumanAllowed()
    {
        var e = At(WorkflowState.AwaitingRequirementApproval);

        var result = Try(e, WorkflowState.ImplementationPlanning, Human);

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(ActorType.Agent)]
    [InlineData(ActorType.N8n)]
    [InlineData(ActorType.Ci)]
    public void Apply_CodeApprovalCreatesPullRequest_OnlyHumanAllowed(ActorType type)
    {
        var e = At(WorkflowState.AwaitingCodeApproval);

        var result = Try(e, WorkflowState.PullRequestCreated, new Actor(type, "x"));

        Assert.Equal(TransitionError.ActorNotAllowed, result.Error);
    }

    [Fact]
    public void Apply_ReviewerApprovesTwice_SecondRejectedAsNotAllowed()
    {
        var e = At(WorkflowState.CodeReview);
        Step(e, WorkflowState.AwaitingCodeApproval, Agent);

        var result = Try(e, WorkflowState.AwaitingCodeApproval, Agent);

        Assert.Equal(TransitionError.NotAllowed, result.Error);
    }

    // ---- Optimistic concurrency ----

    [Fact]
    public void Apply_StaleExpectedVersion_VersionConflictAndNothingChanges()
    {
        var e = NewExecution();
        var stale = new TransitionRequest(WorkflowState.RequirementAnalysis, N8n, ExpectedVersion: 99);

        var result = WorkflowStateMachine.Apply(e, stale, Now);

        Assert.Equal(TransitionError.VersionConflict, result.Error);
        Assert.Equal(WorkflowState.Created, e.CurrentState);
        Assert.Equal(1, e.Version);
    }

    [Fact]
    public void Apply_SameRequestTwice_SecondIsVersionConflict()
    {
        var e = NewExecution();
        var request = new TransitionRequest(WorkflowState.RequirementAnalysis, N8n, ExpectedVersion: 1);

        var first = WorkflowStateMachine.Apply(e, request, Now);
        var second = WorkflowStateMachine.Apply(e, request, Now);

        Assert.True(first.Succeeded);
        Assert.Equal(TransitionError.VersionConflict, second.Error);
        Assert.Equal(2, e.Version);
        Assert.Equal(2, e.History.Count);
    }

    [Fact]
    public void Apply_StaleVersionAndWrongTarget_ReportsVersionConflictFirst()
    {
        var e = NewExecution();
        var request = new TransitionRequest(WorkflowState.Completed, Agent, ExpectedVersion: 99);

        var result = WorkflowStateMachine.Apply(e, request, Now);

        Assert.Equal(TransitionError.VersionConflict, result.Error);
    }

    // ---- Trạng thái cuối ----

    [Theory]
    [MemberData(nameof(AllStates), MemberType = typeof(TestHelpers))]
    public void Apply_FromCompleted_AnyTarget_Rejected(WorkflowState target)
    {
        var e = At(WorkflowState.Completed);

        var result = Try(e, target, Human);

        Assert.Equal(TransitionError.TerminalState, result.Error);
        Assert.Equal(WorkflowState.Completed, e.CurrentState);
    }

    [Theory]
    [MemberData(nameof(AllStates), MemberType = typeof(TestHelpers))]
    public void Apply_FromCancelled_AnyTarget_Rejected(WorkflowState target)
    {
        var e = At(WorkflowState.Coding);
        Step(e, WorkflowState.Cancelled, Human);

        var result = Try(e, target, Human);

        Assert.Equal(TransitionError.TerminalState, result.Error);
    }

    // ---- Vòng lặp có giới hạn ----

    [Fact]
    public void Apply_TestsFailRepeatedly_AllowsThreeFixesThenOnlyFailed()
    {
        var e = At(WorkflowState.BuildAndTest);

        for (var i = 0; i < WorkflowLimits.MaxFixAttempts; i++)
        {
            Step(e, WorkflowState.Coding, Ci);
            Step(e, WorkflowState.BuildAndTest, Agent);
        }

        var again = Try(e, WorkflowState.Coding, Ci);
        var fail = Try(e, WorkflowState.Failed, Ci);

        Assert.Equal(TransitionError.GuardFailed, again.Error);
        Assert.True(fail.Succeeded);
        Assert.Equal(WorkflowState.Failed, e.CurrentState);
        Assert.Equal(WorkflowState.BuildAndTest, e.FailedFromState);
    }

    [Fact]
    public void Apply_BuildFailedBeforeFixesExhausted_CiCannotMarkFailed()
    {
        var e = At(WorkflowState.BuildAndTest);

        var result = Try(e, WorkflowState.Failed, Ci);

        Assert.Equal(TransitionError.GuardFailed, result.Error);
        Assert.Equal(WorkflowState.BuildAndTest, e.CurrentState);
    }

    [Fact]
    public void Apply_ReviewerRequestsChangesRepeatedly_AllowsTwoRoundsThenRejects()
    {
        var e = At(WorkflowState.CodeReview);

        for (var i = 0; i < WorkflowLimits.MaxReviewRounds; i++)
        {
            Step(e, WorkflowState.Coding, Agent);
            Step(e, WorkflowState.BuildAndTest, Agent);
            Step(e, WorkflowState.CodeReview, Ci);
        }

        var result = Try(e, WorkflowState.Coding, Agent);

        Assert.Equal(TransitionError.GuardFailed, result.Error);
        Assert.Equal(WorkflowState.CodeReview, e.CurrentState);
    }

    [Fact]
    public void Apply_RequirementRevisedRepeatedly_AllowsThreeRoundsThenRejects()
    {
        var e = At(WorkflowState.AwaitingRequirementApproval);

        for (var i = 0; i < WorkflowLimits.MaxRequirementRounds; i++)
        {
            Step(e, WorkflowState.RequirementAnalysis, Human);
            Step(e, WorkflowState.AwaitingRequirementApproval, Agent);
        }

        var result = Try(e, WorkflowState.RequirementAnalysis, Human);

        Assert.Equal(TransitionError.GuardFailed, result.Error);
    }

    // ---- Failed và retry ----

    [Theory]
    [MemberData(nameof(NonTerminalStates), MemberType = typeof(TestHelpers))]
    public void Apply_SystemFailsFromAnyLiveState_RemembersWhereItFailed(WorkflowState state)
    {
        if (state == WorkflowState.Failed)
        {
            return; // Failed -> Failed không hợp lệ, có test riêng
        }

        var e = At(state);

        var result = Try(e, WorkflowState.Failed, Sys, "provider down");

        Assert.True(result.Succeeded);
        Assert.Equal(state, e.FailedFromState);
    }

    [Fact]
    public void Apply_FailedToFailed_NotAllowed()
    {
        var e = At(WorkflowState.Coding);
        Step(e, WorkflowState.Failed, Sys);

        var result = Try(e, WorkflowState.Failed, Sys);

        Assert.Equal(TransitionError.NotAllowed, result.Error);
    }

    [Fact]
    public void Apply_RetryFromFailed_ReturnsToFailedStateAndResetsCounters()
    {
        var e = At(WorkflowState.BuildAndTest);
        for (var i = 0; i < WorkflowLimits.MaxFixAttempts; i++)
        {
            Step(e, WorkflowState.Coding, Ci);
            Step(e, WorkflowState.BuildAndTest, Agent);
        }

        Step(e, WorkflowState.Failed, Ci);

        var result = Try(e, WorkflowState.BuildAndTest, Human, "retry");

        Assert.True(result.Succeeded);
        Assert.Equal(WorkflowState.BuildAndTest, e.CurrentState);
        Assert.Null(e.FailedFromState);
        Assert.Equal(0, e.FixAttempts);
    }

    [Fact]
    public void Apply_RetryToDifferentStateThanFailed_GuardFailed()
    {
        var e = At(WorkflowState.BuildAndTest);
        Step(e, WorkflowState.Failed, Sys);

        var result = Try(e, WorkflowState.Coding, Human);

        Assert.Equal(TransitionError.GuardFailed, result.Error);
        Assert.Equal(WorkflowState.Failed, e.CurrentState);
    }

    [Fact]
    public void Apply_RetryByAgent_ActorNotAllowed()
    {
        var e = At(WorkflowState.BuildAndTest);
        Step(e, WorkflowState.Failed, Sys);

        var result = Try(e, WorkflowState.BuildAndTest, Agent);

        Assert.Equal(TransitionError.ActorNotAllowed, result.Error);
    }

    [Fact]
    public void Apply_RetryHistory_KeepsFailedRowAndRetryRow()
    {
        var e = At(WorkflowState.Coding);
        Step(e, WorkflowState.Failed, Sys);
        Step(e, WorkflowState.Coding, N8n);

        var failedRow = e.History[^2];
        var retryRow = e.History[^1];
        Assert.Equal(WorkflowState.Failed, failedRow.ToState);
        Assert.Equal(WorkflowState.Failed, retryRow.FromState);
        Assert.Equal(WorkflowState.Coding, retryRow.ToState);
    }

    // ---- Hủy ----

    [Theory]
    [MemberData(nameof(NonTerminalStates), MemberType = typeof(TestHelpers))]
    public void Apply_HumanCancelsFromAnyLiveState_Succeeds(WorkflowState state)
    {
        var e = state == WorkflowState.Failed ? FailedExecution() : At(state);

        var result = Try(e, WorkflowState.Cancelled, Human, "no longer needed");

        Assert.True(result.Succeeded);
        Assert.Equal(WorkflowState.Cancelled, e.CurrentState);
    }

    [Theory]
    [InlineData(ActorType.Agent)]
    [InlineData(ActorType.N8n)]
    [InlineData(ActorType.Ci)]
    [InlineData(ActorType.System)]
    public void Apply_NonHumanCancels_ActorNotAllowed(ActorType type)
    {
        var e = At(WorkflowState.Coding);

        var result = Try(e, WorkflowState.Cancelled, new Actor(type, "x"));

        Assert.Equal(TransitionError.ActorNotAllowed, result.Error);
    }

    // ---- Bảng ----

    [Fact]
    public void Table_NoRuleLeavesATerminalState()
    {
        Assert.DoesNotContain(TransitionTable.Rules, r => r.From.IsTerminal());
    }

    [Fact]
    public void Table_EveryRuleHasAtLeastOneActorAndNoSelfLoop()
    {
        Assert.All(TransitionTable.Rules, r =>
        {
            Assert.NotEmpty(r.Actors);
            Assert.NotEqual(r.From, r.To);
        });
    }

    [Fact]
    public void Table_OnlyHumanApprovesTheThreeGates()
    {
        AssertOnlyHuman(WorkflowState.AwaitingRequirementApproval, WorkflowState.ImplementationPlanning);
        AssertOnlyHuman(WorkflowState.AwaitingCodeApproval, WorkflowState.PullRequestCreated);

        static void AssertOnlyHuman(WorkflowState from, WorkflowState to)
        {
            var rules = TransitionTable.Rules.Where(r => r.From == from && r.To == to).ToList();
            var rule = Assert.Single(rules);
            Assert.Equal([ActorType.Human], rule.Actors);
        }
    }

    // ---- Retry (không chọn đích, server tự quay về FailedFromState) ----

    [Fact]
    public void Retry_FailedExecution_ReturnsToFailedFromStateSameExecution()
    {
        var e = FailedExecution();
        var id = e.Id;
        var version = e.Version;

        var result = WorkflowStateMachine.Retry(e, Human, e.Version, null, Now);

        Assert.True(result.Succeeded);
        Assert.Equal(id, e.Id);
        Assert.Equal(WorkflowState.Coding, e.CurrentState);
        Assert.Null(e.FailedFromState);
        Assert.Equal(version + 1, e.Version);
        var last = e.History[^1];
        Assert.Equal(WorkflowState.Failed, last.FromState);
        Assert.Equal(WorkflowState.Coding, last.ToState);
        Assert.Equal("Retry", last.Reason);
    }

    [Theory]
    [MemberData(nameof(NonTerminalStates), MemberType = typeof(TestHelpers))]
    public void Retry_NotFailed_NotAllowedAndNothingChanges(WorkflowState state)
    {
        if (state == WorkflowState.Failed)
        {
            return;
        }

        var e = At(state);
        var before = (e.CurrentState, e.Version, e.History.Count);

        var result = WorkflowStateMachine.Retry(e, Human, e.Version, null, Now);

        Assert.Equal(TransitionError.NotAllowed, result.Error);
        Assert.Equal(before, (e.CurrentState, e.Version, e.History.Count));
    }

    [Fact]
    public void Retry_CompletedExecution_NotAllowed()
    {
        var e = At(WorkflowState.Completed);

        var result = WorkflowStateMachine.Retry(e, Human, e.Version, null, Now);

        Assert.Equal(TransitionError.NotAllowed, result.Error);
        Assert.Equal(WorkflowState.Completed, e.CurrentState);
    }

    [Fact]
    public void Retry_StaleVersion_VersionConflict()
    {
        var e = FailedExecution();

        var result = WorkflowStateMachine.Retry(e, Human, e.Version - 1, null, Now);

        Assert.Equal(TransitionError.VersionConflict, result.Error);
        Assert.Equal(WorkflowState.Failed, e.CurrentState);
    }

    [Fact]
    public void Retry_ByAgentOrSystem_ActorNotAllowed()
    {
        var e = FailedExecution();

        Assert.Equal(TransitionError.ActorNotAllowed, WorkflowStateMachine.Retry(e, Agent, e.Version, null, Now).Error);
        Assert.Equal(TransitionError.ActorNotAllowed, WorkflowStateMachine.Retry(e, Sys, e.Version, null, Now).Error);
        Assert.Equal(WorkflowState.Failed, e.CurrentState);
    }

    [Fact]
    public void Retry_ByN8n_Allowed()
    {
        var e = FailedExecution();

        Assert.True(WorkflowStateMachine.Retry(e, N8n, e.Version, "auto retry", Now).Succeeded);
        Assert.Equal("auto retry", e.History[^1].Reason);
    }

    [Fact]
    public void Retry_SameRequestTwice_SecondIsVersionConflict()
    {
        var e = FailedExecution();
        var version = e.Version;

        var first = WorkflowStateMachine.Retry(e, Human, version, null, Now);
        var second = WorkflowStateMachine.Retry(e, Human, version, null, Now);

        Assert.True(first.Succeeded);
        Assert.Equal(TransitionError.VersionConflict, second.Error);
        Assert.Single(e.History, h => h.FromState == WorkflowState.Failed);
    }

    private static WorkflowExecution FailedExecution()
    {
        var e = At(WorkflowState.Coding);
        Step(e, WorkflowState.Failed, Sys);
        return e;
    }
}
