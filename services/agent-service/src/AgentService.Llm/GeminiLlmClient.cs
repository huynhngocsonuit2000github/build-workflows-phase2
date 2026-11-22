using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgentService.Llm;

/// <summary>
/// Client implementation that performs single HTTP calls to the Google Gemini API.
/// </summary>
public class GeminiLlmClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly LlmOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<GeminiLlmClient>? _logger;

    public GeminiLlmClient(
        HttpClient httpClient,
        IOptions<LlmOptions> options,
        TimeProvider? timeProvider = null,
        ILogger<GeminiLlmClient>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger;
    }

    public async Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ct.ThrowIfCancellationRequested();

        var model = !string.IsNullOrWhiteSpace(request.ModelOverride)
            ? request.ModelOverride
            : (!string.IsNullOrWhiteSpace(_options.PrimaryModel) ? _options.PrimaryModel : "gemini-3.8-flash");

        var baseUrl = (_options.BaseUrl ?? "https://generativelanguage.googleapis.com").TrimEnd('/');
        var requestUri = $"{baseUrl}/v1beta/models/{Uri.EscapeDataString(model)}:generateContent";

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri);

        // API key MUST always be sent via header, never in the URL
        httpRequest.Headers.TryAddWithoutValidation("x-goog-api-key", _options.ApiKey ?? string.Empty);

        var payload = BuildPayload(request);
        var json = JsonSerializer.Serialize(payload, GeminiJsonContext.DefaultOptions);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var startTimestamp = _timeProvider.GetTimestamp();
        HttpResponseMessage httpResponse;

        try
        {
            httpResponse = await _httpClient.SendAsync(httpRequest, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Caller cancellation: do not wrap, propagate immediately
            throw;
        }
        catch (HttpRequestException ex)
        {
            var sanitizedMessage = LlmException.Sanitize(ex.Message, _options.ApiKey);
            throw new LlmException(
                sanitizedMessage,
                LlmErrorKind.Transient,
                httpStatus: (int?)ex.StatusCode,
                retryable: true,
                model: model,
                attempts: 1,
                innerException: ex);
        }

        var latency = _timeProvider.GetElapsedTime(startTimestamp);
        using (httpResponse)
        {
            var statusCode = (int)httpResponse.StatusCode;
            var responseBody = await httpResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!httpResponse.IsSuccessStatusCode)
            {
                var kind = MapStatusCodeToErrorKind(statusCode);
                var isRetryable = LlmException.IsDefaultRetryable(kind);
                var retryAfter = ExtractRetryAfter(httpResponse);

                var errorDetail = TryExtractErrorMessage(responseBody) ?? $"Gemini API returned HTTP {statusCode}.";
                var sanitizedDetail = LlmException.Sanitize(errorDetail, _options.ApiKey);

                throw new LlmException(
                    sanitizedDetail,
                    kind,
                    httpStatus: statusCode,
                    retryable: isRetryable,
                    retryAfter: retryAfter,
                    model: model,
                    attempts: 1);
            }

            return ParseSuccessResponse(responseBody, model, latency);
        }
    }

    private static GeminiRequestPayload BuildPayload(LlmRequest request)
    {
        var payload = new GeminiRequestPayload();

        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
        {
            payload.SystemInstruction = new GeminiContent
            {
                Parts = [new GeminiPart { Text = request.SystemPrompt }]
            };
        }

        payload.Contents.Add(new GeminiContent
        {
            Role = "user",
            Parts = [new GeminiPart { Text = request.UserPrompt }]
        });

        if (request.Temperature.HasValue || request.MaxOutputTokens.HasValue || request.JsonOutput)
        {
            payload.GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = request.Temperature,
                MaxOutputTokens = request.MaxOutputTokens,
                ResponseMimeType = request.JsonOutput ? "application/json" : null
            };
        }

        return payload;
    }

    private LlmResponse ParseSuccessResponse(string responseBody, string model, TimeSpan latency)
    {
        GeminiResponsePayload? geminiResponse = null;
        try
        {
            geminiResponse = JsonSerializer.Deserialize<GeminiResponsePayload>(
                responseBody,
                GeminiJsonContext.DefaultOptions);
        }
        catch (Exception ex)
        {
            throw new LlmException(
                "Failed to deserialize Gemini response JSON.",
                LlmErrorKind.Transient,
                httpStatus: 200,
                retryable: true,
                model: model,
                attempts: 1,
                innerException: ex);
        }

        if (geminiResponse?.Candidates == null || geminiResponse.Candidates.Count == 0)
        {
            throw new LlmException(
                "Gemini returned an empty candidate list.",
                LlmErrorKind.EmptyOrBlocked,
                httpStatus: 200,
                retryable: false,
                model: model,
                attempts: 1);
        }

        var candidate = geminiResponse.Candidates[0];
        var finishReason = candidate.FinishReason?.ToUpperInvariant();
        if (finishReason is "SAFETY" or "BLOCKED" or "PROHIBITED_CONTENT" or "SPII" or "RECITATION")
        {
            throw new LlmException(
                $"Gemini candidate was blocked by safety filters. Finish reason: {candidate.FinishReason}.",
                LlmErrorKind.EmptyOrBlocked,
                httpStatus: 200,
                retryable: false,
                model: model,
                attempts: 1);
        }

        var parts = candidate.Content?.Parts;
        if (parts == null || parts.Count == 0)
        {
            throw new LlmException(
                "Gemini candidate content has no parts.",
                LlmErrorKind.EmptyOrBlocked,
                httpStatus: 200,
                retryable: false,
                model: model,
                attempts: 1);
        }

        var sb = new StringBuilder();
        foreach (var part in parts)
        {
            if (!string.IsNullOrEmpty(part.Text))
            {
                sb.Append(part.Text);
            }
        }

        var text = sb.ToString();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new LlmException(
                "Gemini returned empty text response.",
                LlmErrorKind.EmptyOrBlocked,
                httpStatus: 200,
                retryable: false,
                model: model,
                attempts: 1);
        }

        var inputTokens = geminiResponse.UsageMetadata?.PromptTokenCount;
        var outputTokens = geminiResponse.UsageMetadata?.CandidatesTokenCount;

        _logger?.LogDebug(
            "Gemini invocation successful: model={Model}, latency={LatencyMs}ms, inTokens={InputTokens}, outTokens={OutputTokens}",
            model,
            latency.TotalMilliseconds,
            inputTokens,
            outputTokens);

        return new LlmResponse(
            Text: text,
            ModelUsed: model,
            UsedFallback: false,
            Attempts: 1,
            InputTokens: inputTokens,
            OutputTokens: outputTokens,
            Latency: latency,
            FinishReason: candidate.FinishReason);
    }

    public static LlmErrorKind MapStatusCodeToErrorKind(int statusCode) => statusCode switch
    {
        400 => LlmErrorKind.Validation,
        401 => LlmErrorKind.Unauthorized,
        403 => LlmErrorKind.Unauthorized,
        404 => LlmErrorKind.Validation,
        429 => LlmErrorKind.RateLimited,
        500 or 502 or 503 or 504 => LlmErrorKind.Transient,
        _ => LlmErrorKind.Permanent
    };

    private TimeSpan? ExtractRetryAfter(HttpResponseMessage response)
    {
        if (response.Headers.RetryAfter != null)
        {
            if (response.Headers.RetryAfter.Delta.HasValue)
            {
                return response.Headers.RetryAfter.Delta.Value;
            }
            if (response.Headers.RetryAfter.Date.HasValue)
            {
                var diff = response.Headers.RetryAfter.Date.Value - _timeProvider.GetUtcNow();
                return diff > TimeSpan.Zero ? diff : TimeSpan.Zero;
            }
        }

        if (response.Headers.TryGetValues("Retry-After", out var values))
        {
            var raw = values.FirstOrDefault();
            if (int.TryParse(raw, out var sec))
            {
                return TimeSpan.FromSeconds(sec);
            }
        }

        return null;
    }

    private static string? TryExtractErrorMessage(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("error", out var errorElement))
            {
                if (errorElement.TryGetProperty("message", out var msgElement))
                {
                    return msgElement.GetString();
                }
            }
        }
        catch
        {
            // Ignore JSON parse errors for non-JSON response bodies
        }

        return responseBody.Length > 200 ? responseBody[..200] : responseBody;
    }

    #region Internal Gemini DTOs
    internal static class GeminiJsonContext
    {
        public static readonly JsonSerializerOptions DefaultOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    internal sealed class GeminiRequestPayload
    {
        [JsonPropertyName("contents")]
        public List<GeminiContent> Contents { get; set; } = [];

        [JsonPropertyName("systemInstruction")]
        public GeminiContent? SystemInstruction { get; set; }

        [JsonPropertyName("generationConfig")]
        public GeminiGenerationConfig? GenerationConfig { get; set; }
    }

    internal sealed class GeminiContent
    {
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; } = [];
    }

    internal sealed class GeminiPart
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    internal sealed class GeminiGenerationConfig
    {
        [JsonPropertyName("temperature")]
        public double? Temperature { get; set; }

        [JsonPropertyName("maxOutputTokens")]
        public int? MaxOutputTokens { get; set; }

        [JsonPropertyName("responseMimeType")]
        public string? ResponseMimeType { get; set; }
    }

    internal sealed class GeminiResponsePayload
    {
        [JsonPropertyName("candidates")]
        public List<GeminiCandidate>? Candidates { get; set; }

        [JsonPropertyName("usageMetadata")]
        public GeminiUsageMetadata? UsageMetadata { get; set; }
    }

    internal sealed class GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContent? Content { get; set; }

        [JsonPropertyName("finishReason")]
        public string? FinishReason { get; set; }
    }

    internal sealed class GeminiUsageMetadata
    {
        [JsonPropertyName("promptTokenCount")]
        public int? PromptTokenCount { get; set; }

        [JsonPropertyName("candidatesTokenCount")]
        public int? CandidatesTokenCount { get; set; }

        [JsonPropertyName("totalTokenCount")]
        public int? TotalTokenCount { get; set; }
    }
    #endregion
}
