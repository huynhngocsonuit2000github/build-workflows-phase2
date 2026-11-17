namespace WorkflowApi.Domain;

/// <summary>
/// Một lần chạy workflow. Trạng thái chỉ đổi qua <see cref="WorkflowStateMachine.Apply"/>;
/// các setter đều internal để không chỗ nào khác ghi trực tiếp được.
/// </summary>
public sealed class WorkflowExecution
{
    private readonly List<WorkflowHistory> _history = [];

    private WorkflowExecution()
    {
    }

    public Guid Id { get; private set; }
    public string JiraIssueKey { get; private set; } = "";
    public string CorrelationId { get; private set; } = "";
    public WorkflowState CurrentState { get; private set; }

    /// <summary>Tăng 1 sau mỗi chuyển thành công; dùng cho optimistic concurrency (ExpectedVersion).</summary>
    public long Version { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    /// <summary>Trạng thái đã hỏng, để retry quay về đúng chỗ. Chỉ có giá trị khi đang ở Failed.</summary>
    public WorkflowState? FailedFromState { get; internal set; }

    public int RequirementRounds { get; internal set; }
    public int FixAttempts { get; internal set; }
    public int ReviewRounds { get; internal set; }

    public IReadOnlyList<WorkflowHistory> History => _history;

    public static WorkflowExecution Create(string jiraIssueKey, string correlationId, Actor creator, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(jiraIssueKey))
        {
            throw new ArgumentException("Jira issue key is required.", nameof(jiraIssueKey));
        }

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            throw new ArgumentException("Correlation id is required.", nameof(correlationId));
        }

        ArgumentNullException.ThrowIfNull(creator);

        var execution = new WorkflowExecution
        {
            Id = Guid.NewGuid(),
            JiraIssueKey = jiraIssueKey.Trim(),
            CorrelationId = correlationId.Trim(),
            CurrentState = WorkflowState.Created,
            Version = 1,
            CreatedAt = now,
            UpdatedAt = now
        };

        execution._history.Add(new WorkflowHistory(null, WorkflowState.Created, now, creator.ToString(), "Created"));
        return execution;
    }

    /// <summary>Ghi chuyển trạng thái. Chỉ state machine được gọi (internal).</summary>
    internal void Commit(WorkflowState to, Actor actor, string? reason, DateTime now)
    {
        _history.Add(new WorkflowHistory(CurrentState, to, now, actor.ToString(), reason));
        CurrentState = to;
        Version++;
        UpdatedAt = now;
    }
}
