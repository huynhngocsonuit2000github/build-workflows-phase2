namespace WorkflowApi.Domain;

/// <summary>Giá trị khởi đầu của các vòng lặp có giới hạn. Chốt lại khi đo thật (s1-5, s12).</summary>
public static class WorkflowLimits
{
    /// <summary>Số lần người trả lời/yêu cầu sửa rồi quay về RequirementAnalysis.</summary>
    public const int MaxRequirementRounds = 3;

    /// <summary>Số lần quay về Coding sau khi test đỏ.</summary>
    public const int MaxFixAttempts = 3;

    /// <summary>Số vòng Reviewer yêu cầu sửa rồi quay về Coding.</summary>
    public const int MaxReviewRounds = 2;
}
