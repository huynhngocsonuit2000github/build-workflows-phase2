namespace WorkflowApi.Domain;

/// <summary>Một dòng lịch sử, chỉ thêm, không sửa. FromState rỗng ở dòng tạo lần chạy.</summary>
public sealed record WorkflowHistory(
    WorkflowState? FromState,
    WorkflowState ToState,
    DateTime ChangedAt,
    string ChangedBy,
    string? Reason);
