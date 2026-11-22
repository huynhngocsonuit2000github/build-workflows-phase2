namespace AgentService.Llm;

/// <summary>
/// Response contract returned by an LLM client invocation.
/// </summary>
public sealed record LlmResponse(
    string Text,
    string ModelUsed,
    bool UsedFallback,
    int Attempts,
    int? InputTokens,
    int? OutputTokens,
    TimeSpan Latency,
    string? FinishReason = null);
