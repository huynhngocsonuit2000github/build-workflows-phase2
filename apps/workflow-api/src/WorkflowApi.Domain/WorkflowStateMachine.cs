namespace WorkflowApi.Domain;

public sealed record TransitionRequest(
    WorkflowState To,
    Actor Actor,
    long ExpectedVersion,
    string? Reason = null);

public enum TransitionError
{
    None,

    /// <summary>ExpectedVersion không khớp: ai đó đã đổi trước (sau này ánh xạ sang HTTP 409).</summary>
    VersionConflict,

    /// <summary>Đã ở trạng thái cuối.</summary>
    TerminalState,

    /// <summary>Cặp (từ, sang) không có trong bảng.</summary>
    NotAllowed,

    /// <summary>Cặp có trong bảng nhưng loại actor này không được kích hoạt.</summary>
    ActorNotAllowed,

    /// <summary>Cặp và actor đúng nhưng điều kiện chưa thỏa (ví dụ hết/chưa hết lượt sửa).</summary>
    GuardFailed
}

public sealed record TransitionResult(bool Succeeded, TransitionError Error, string? Message)
{
    public static TransitionResult Ok() => new(true, TransitionError.None, null);

    public static TransitionResult Reject(TransitionError error, string message) => new(false, error, message);
}

/// <summary>
/// Chỗ DUY NHẤT đổi trạng thái của một WorkflowExecution.
/// Thứ tự kiểm tra: version, trạng thái cuối, cặp (từ, sang), loại actor, điều kiện.
/// Từ chối thì không ghi gì (trạng thái, version, lịch sử giữ nguyên).
/// </summary>
public static class WorkflowStateMachine
{
    public static TransitionResult Apply(WorkflowExecution execution, TransitionRequest request, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(execution);
        ArgumentNullException.ThrowIfNull(request);

        if (execution.Version != request.ExpectedVersion)
        {
            return TransitionResult.Reject(
                TransitionError.VersionConflict,
                $"Expected version {request.ExpectedVersion} but current is {execution.Version}.");
        }

        var from = execution.CurrentState;
        if (from.IsTerminal())
        {
            return TransitionResult.Reject(TransitionError.TerminalState, $"{from} is a terminal state.");
        }

        var candidates = TransitionTable.Rules.Where(r => r.From == from && r.To == request.To).ToList();
        if (candidates.Count == 0)
        {
            return TransitionResult.Reject(TransitionError.NotAllowed, $"{from} -> {request.To} is not a valid transition.");
        }

        var byActor = candidates.Where(r => r.Actors.Contains(request.Actor.Type)).ToList();
        if (byActor.Count == 0)
        {
            return TransitionResult.Reject(
                TransitionError.ActorNotAllowed,
                $"{request.Actor.Type} cannot trigger {from} -> {request.To}.");
        }

        var rule = byActor.FirstOrDefault(r => r.Guard is null || r.Guard(execution));
        if (rule is null)
        {
            var message = byActor[0].GuardMessage ?? "Transition condition is not met.";
            return TransitionResult.Reject(TransitionError.GuardFailed, message);
        }

        rule.Effect?.Invoke(execution);
        execution.Commit(request.To, request.Actor, request.Reason, now);
        return TransitionResult.Ok();
    }

    /// <summary>
    /// Retry một lần chạy đang Failed: quay về đúng trạng thái đã hỏng (FailedFromState).
    /// Người gọi không chọn đích, nên không retry nhầm chỗ. Vẫn đi qua <see cref="Apply"/>
    /// nên vẫn kiểm version, loại actor (Human, N8n) và ghi lịch sử. Không tạo lần chạy mới.
    /// </summary>
    public static TransitionResult Retry(
        WorkflowExecution execution, Actor actor, long expectedVersion, string? reason, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(execution);
        ArgumentNullException.ThrowIfNull(actor);

        if (execution.Version != expectedVersion)
        {
            return TransitionResult.Reject(
                TransitionError.VersionConflict,
                $"Expected version {expectedVersion} but current is {execution.Version}.");
        }

        if (execution.CurrentState != WorkflowState.Failed || execution.FailedFromState is null)
        {
            return TransitionResult.Reject(
                TransitionError.NotAllowed,
                $"Only a Failed execution can be retried; current state is {execution.CurrentState}.");
        }

        var request = new TransitionRequest(
            execution.FailedFromState.Value, actor, expectedVersion, string.IsNullOrWhiteSpace(reason) ? "Retry" : reason);
        return Apply(execution, request, now);
    }
}
