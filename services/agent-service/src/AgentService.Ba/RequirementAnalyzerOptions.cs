namespace AgentService.Ba;

/// <summary>
/// Configuration options for the RequirementAnalyzer pipeline.
/// </summary>
public sealed class RequirementAnalyzerOptions
{
    /// <summary>
    /// Maximum number of generation attempts (initial attempt + retries).
    /// Default is 3. Must be at least 1.
    /// </summary>
    public int MaxGenerationAttempts { get; set; } = 3;

    /// <summary>
    /// LLM sampling temperature. Default is 0.2 (low temperature for deterministic structured output).
    /// </summary>
    public double Temperature { get; set; } = 0.2;

    /// <summary>
    /// Maximum tokens for the completion response. Default is 2048.
    /// </summary>
    public int MaxOutputTokens { get; set; } = 2048;

    /// <summary>
    /// Optional model override. If null, uses the LLM client's configured primary model.
    /// </summary>
    public string? ModelOverride { get; set; }

    /// <summary>
    /// Optional explicit path to the prompt markdown file.
    /// If null, resolved using RepoPathResolver.
    /// </summary>
    public string? PromptPath { get; set; }

    /// <summary>
    /// Optional explicit path to the JSON schema file.
    /// If null, resolved using RepoPathResolver.
    /// </summary>
    public string? SchemaPath { get; set; }
}
