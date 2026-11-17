namespace WorkflowApi.Domain;

/// <summary>
/// Một dòng của bảng chuyển hợp lệ: từ đâu, sang đâu, ai được kích hoạt,
/// điều kiện thêm (guard), và tác dụng phụ lên bộ đếm (effect, chạy trước khi đổi trạng thái).
/// </summary>
public sealed record TransitionRule(
    WorkflowState From,
    WorkflowState To,
    IReadOnlyList<ActorType> Actors,
    Func<WorkflowExecution, bool>? Guard = null,
    string? GuardMessage = null,
    Action<WorkflowExecution>? Effect = null);
