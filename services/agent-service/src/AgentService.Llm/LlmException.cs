using System.Text.RegularExpressions;

namespace AgentService.Llm;

/// <summary>
/// Exception representing an error occurring during LLM invocation.
/// </summary>
public partial class LlmException : Exception
{
    private static readonly Regex ApiKeyPattern = new(@"AIza[0-9A-Za-z_-]{35}", RegexOptions.Compiled);

    public LlmErrorKind Kind { get; }
    public int? HttpStatus { get; }
    public bool Retryable { get; }
    public TimeSpan? RetryAfter { get; }
    public string? Model { get; }
    public int Attempts { get; init; }

    public LlmException(
        string message,
        LlmErrorKind kind,
        int? httpStatus = null,
        bool? retryable = null,
        TimeSpan? retryAfter = null,
        string? model = null,
        int attempts = 1,
        Exception? innerException = null)
        : base(Sanitize(message), innerException)
    {
        Kind = kind;
        HttpStatus = httpStatus;
        Retryable = retryable ?? IsDefaultRetryable(kind);
        RetryAfter = retryAfter;
        Model = model;
        Attempts = attempts;
    }

    /// <summary>
    /// Determines whether an error kind is retryable by default.
    /// </summary>
    public static bool IsDefaultRetryable(LlmErrorKind kind) => kind switch
    {
        LlmErrorKind.RateLimited => true,
        LlmErrorKind.Transient => true,
        LlmErrorKind.Timeout => true,
        _ => false
    };

    /// <summary>
    /// Sanitizes message text to prevent accidental exposure of API keys or credentials.
    /// </summary>
    public static string Sanitize(string message, string? apiKey = null)
    {
        if (string.IsNullOrEmpty(message))
        {
            return message;
        }

        var sanitized = message;
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            sanitized = sanitized.Replace(apiKey, "[REDACTED]");
        }

        return ApiKeyPattern.Replace(sanitized, "[REDACTED]");
    }
}
