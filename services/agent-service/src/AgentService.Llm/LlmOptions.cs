namespace AgentService.Llm;

/// <summary>
/// Configuration options for LLM client resilience, models, and provider endpoints.
/// </summary>
public sealed class LlmOptions
{
    public const string SectionName = "Llm";

    /// <summary>
    /// The primary LLM model identifier.
    /// Default: "gemini-3.8-flash".
    /// </summary>
    public string PrimaryModel { get; set; } = "gemini-3.8-flash";

    /// <summary>
    /// The fallback LLM model identifier when the primary exhausts retryable attempts.
    /// Can be null or empty to disable fallback.
    /// Default: "gemini-3.5-flash-lite".
    /// </summary>
    public string? FallbackModel { get; set; } = "gemini-3.5-flash-lite";

    /// <summary>
    /// Timeout allocated for each individual invocation attempt.
    /// Default: 60 seconds.
    /// </summary>
    public TimeSpan PerAttemptTimeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Maximum attempts permitted per model (1 initial + retries).
    /// Default: 3.
    /// </summary>
    public int MaxAttemptsPerModel { get; set; } = 3;

    /// <summary>
    /// Base delay for exponential backoff calculation.
    /// Default: 1 second.
    /// </summary>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Maximum delay cap for exponential backoff.
    /// Default: 20 seconds.
    /// </summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Gemini API key passed via the x-goog-api-key header.
    /// Should be loaded from environment or secrets, never hardcoded.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Base URL for the Gemini API.
    /// Default: "https://generativelanguage.googleapis.com".
    /// </summary>
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com";

    /// <summary>
    /// Whether to add random jitter to exponential backoff delays.
    /// Default: true.
    /// </summary>
    public bool EnableJitter { get; set; } = true;
}
