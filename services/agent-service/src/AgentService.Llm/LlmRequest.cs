namespace AgentService.Llm;

/// <summary>
/// Request contract for LLM text completion.
/// </summary>
public sealed record LlmRequest(
    string SystemPrompt,
    string UserPrompt,
    string? ModelOverride = null,
    double? Temperature = null,
    int? MaxOutputTokens = null,
    string? CorrelationId = null,
    bool JsonOutput = false);
