namespace AgentService.Llm;

/// <summary>
/// Core contract for LLM invocation across the platform.
/// </summary>
public interface ILlmClient
{
    /// <summary>
    /// Executes a completion request against the configured LLM.
    /// </summary>
    /// <param name="request">The completion request details.</param>
    /// <param name="ct">Cancellation token from the caller.</param>
    /// <returns>The LLM response.</returns>
    Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct = default);
}
