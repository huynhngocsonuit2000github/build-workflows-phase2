namespace AgentService.Llm;

/// <summary>
/// Categorized classification of errors encountered during LLM calls.
/// </summary>
public enum LlmErrorKind
{
    /// <summary>
    /// Bad request, schema mismatch, invalid model name (e.g. 400, 404). Non-retryable.
    /// </summary>
    Validation,

    /// <summary>
    /// Authentication or permission failure (e.g. 401, 403). Non-retryable.
    /// </summary>
    Unauthorized,

    /// <summary>
    /// Rate limit exceeded or quota exhausted (e.g. 429). Retryable with backoff.
    /// </summary>
    RateLimited,

    /// <summary>
    /// Temporary provider server error (e.g. 500, 502, 503, 504) or network failure. Retryable.
    /// </summary>
    Transient,

    /// <summary>
    /// Timeout on an individual attempt. Retryable.
    /// </summary>
    Timeout,

    /// <summary>
    /// Response 200 returned but output was empty or blocked by safety filters. Non-retryable.
    /// </summary>
    EmptyOrBlocked,

    /// <summary>
    /// Other unclassified non-retryable provider errors.
    /// </summary>
    Permanent
}
