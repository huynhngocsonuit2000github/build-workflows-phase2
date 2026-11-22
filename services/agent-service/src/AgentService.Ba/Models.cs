namespace AgentService.Ba;

public sealed record TicketInput(string Key, string Title, string Description);

public enum AnalysisStatus
{
    Valid,
    NeedsHuman
}

public sealed record RequirementAnalysis(
    string Summary,
    string UserStory,
    IReadOnlyList<string> AcceptanceCriteria,
    IReadOnlyList<string> OpenQuestions,
    IReadOnlyList<string> Assumptions);

public sealed record AnalysisResult(
    AnalysisStatus Status,
    RequirementAnalysis? Analysis,
    string PromptName,
    int PromptVersion,
    string SchemaVersion,
    IReadOnlyList<string> ModelsUsed,
    int GenerationAttempts,
    int TotalInputTokens,
    int TotalOutputTokens,
    TimeSpan TotalLatency,
    IReadOnlyList<string> Failures);

public interface IRequirementAnalyzer
{
    Task<AnalysisResult> AnalyzeAsync(TicketInput ticket, CancellationToken ct = default);
}
