using System.Net;
using AgentService.Llm.Tests.TestHelpers;
using Microsoft.Extensions.Options;

namespace AgentService.Llm.Tests;

public class GeminiLlmClientTests
{
    private readonly LlmOptions _defaultOptions = new()
    {
        ApiKey = "test-secret-key-12345",
        PrimaryModel = "gemini-3.8-flash",
        FallbackModel = "gemini-3.5-flash-lite",
        BaseUrl = "https://mock.generativelanguage.googleapis.com"
    };

    [Fact]
    public async Task CompleteAsync_Valid200Response_ReturnsExpectedTextTokensAndModel()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();
        var validJson = """
        {
          "candidates": [
            {
              "content": {
                "parts": [
                  { "text": "Hello from Gemini!" }
                ],
                "role": "model"
              },
              "finishReason": "STOP"
            }
          ],
          "usageMetadata": {
            "promptTokenCount": 12,
            "candidatesTokenCount": 24,
            "totalTokenCount": 36
          }
        }
        """;
        mockHandler.EnqueueResponse(HttpStatusCode.OK, validJson);

        using var httpClient = new HttpClient(mockHandler);
        var client = new GeminiLlmClient(httpClient, Options.Create(_defaultOptions));

        var request = new LlmRequest("You are a helpful assistant", "Hello");

        // Act
        var response = await client.CompleteAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("Hello from Gemini!", response.Text);
        Assert.Equal("gemini-3.8-flash", response.ModelUsed);
        Assert.False(response.UsedFallback);
        Assert.Equal(1, response.Attempts);
        Assert.Equal(12, response.InputTokens);
        Assert.Equal(24, response.OutputTokens);
        Assert.True(response.Latency >= TimeSpan.Zero);
    }

    [Fact]
    public async Task CompleteAsync_HeadersAndUrl_KeySentInHeader_NotInUrl_NotLeakedInException()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();
        var secretKey = "test-secret-key-12345";
        var options = new LlmOptions
        {
            ApiKey = secretKey,
            PrimaryModel = "gemini-3.8-flash",
            BaseUrl = "https://mock.generativelanguage.googleapis.com"
        };

        // Response with error containing the key accidentally in the response body
        var errorBodyWithKey = $"{{\"error\": {{\"code\": 401, \"message\": \"Invalid key: {secretKey}\"}}}}";
        mockHandler.EnqueueResponse(HttpStatusCode.Unauthorized, errorBodyWithKey);

        using var httpClient = new HttpClient(mockHandler);
        var client = new GeminiLlmClient(httpClient, Options.Create(options));
        var request = new LlmRequest("System", "User");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<LlmException>(() => client.CompleteAsync(request));

        // Check request headers and URI
        Assert.Single(mockHandler.SentRequests);
        var sent = mockHandler.SentRequests[0];

        Assert.True(sent.Headers.Contains("x-goog-api-key"));
        Assert.Equal(secretKey, sent.Headers.GetValues("x-goog-api-key").First());

        var urlString = sent.RequestUri?.ToString() ?? string.Empty;
        Assert.DoesNotContain(secretKey, urlString);

        // Check exception message does NOT leak the key
        Assert.DoesNotContain(secretKey, ex.Message);
        Assert.Contains("[REDACTED]", ex.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, LlmErrorKind.Validation, false)]
    [InlineData(HttpStatusCode.Unauthorized, LlmErrorKind.Unauthorized, false)]
    [InlineData(HttpStatusCode.Forbidden, LlmErrorKind.Unauthorized, false)]
    [InlineData(HttpStatusCode.NotFound, LlmErrorKind.Validation, false)]
    [InlineData(HttpStatusCode.InternalServerError, LlmErrorKind.Transient, true)]
    [InlineData(HttpStatusCode.BadGateway, LlmErrorKind.Transient, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, LlmErrorKind.Transient, true)]
    [InlineData(HttpStatusCode.GatewayTimeout, LlmErrorKind.Transient, true)]
    public async Task CompleteAsync_StatusCodes_MapToCorrectLlmErrorKindAndRetryable(
        HttpStatusCode statusCode,
        LlmErrorKind expectedKind,
        bool expectedRetryable)
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.EnqueueResponse(statusCode, """{"error": {"message": "Provider failed"}}""");

        using var httpClient = new HttpClient(mockHandler);
        var client = new GeminiLlmClient(httpClient, Options.Create(_defaultOptions));
        var request = new LlmRequest("System", "Prompt");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<LlmException>(() => client.CompleteAsync(request));

        Assert.Equal(expectedKind, ex.Kind);
        Assert.Equal((int)statusCode, ex.HttpStatus);
        Assert.Equal(expectedRetryable, ex.Retryable);
    }

    [Fact]
    public async Task CompleteAsync_RateLimited429_ReadsRetryAfterHeader()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.EnqueueResponse(
            (HttpStatusCode)429,
            """{"error": {"message": "Resource exhausted"}}""",
            retryAfterHeader: "15");

        using var httpClient = new HttpClient(mockHandler);
        var client = new GeminiLlmClient(httpClient, Options.Create(_defaultOptions));
        var request = new LlmRequest("System", "Prompt");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<LlmException>(() => client.CompleteAsync(request));

        Assert.Equal(LlmErrorKind.RateLimited, ex.Kind);
        Assert.Equal(429, ex.HttpStatus);
        Assert.True(ex.Retryable);
        Assert.NotNull(ex.RetryAfter);
        Assert.Equal(TimeSpan.FromSeconds(15), ex.RetryAfter.Value);
    }

    [Theory]
    [InlineData("""{"candidates": []}""")]
    [InlineData("""{"candidates": [{"finishReason": "SAFETY", "content": {"parts": []}}]}""")]
    [InlineData("""{"candidates": [{"finishReason": "BLOCKED", "content": {"parts": [{"text": "blocked"}]}}]}""")]
    [InlineData("""{"candidates": [{"finishReason": "STOP", "content": {"parts": [{"text": "   "}]}}]}""")]
    public async Task CompleteAsync_200EmptyOrBlocked_ThrowsEmptyOrBlockedNonRetryable(string jsonResponse)
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.EnqueueResponse(HttpStatusCode.OK, jsonResponse);

        using var httpClient = new HttpClient(mockHandler);
        var client = new GeminiLlmClient(httpClient, Options.Create(_defaultOptions));
        var request = new LlmRequest("System", "Prompt");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<LlmException>(() => client.CompleteAsync(request));

        Assert.Equal(LlmErrorKind.EmptyOrBlocked, ex.Kind);
        Assert.Equal(200, ex.HttpStatus);
        Assert.False(ex.Retryable);
    }

    [Fact]
    public async Task CompleteAsync_NetworkFailure_ThrowsTransientException()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.EnqueueException(new HttpRequestException("DNS resolution failed"));

        using var httpClient = new HttpClient(mockHandler);
        var client = new GeminiLlmClient(httpClient, Options.Create(_defaultOptions));
        var request = new LlmRequest("System", "Prompt");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<LlmException>(() => client.CompleteAsync(request));

        Assert.Equal(LlmErrorKind.Transient, ex.Kind);
        Assert.True(ex.Retryable);
        Assert.IsType<HttpRequestException>(ex.InnerException);
    }

    [Fact]
    public async Task CompleteAsync_FinishReason_PopulatedInResponse()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();
        var json = """
        {
          "candidates": [
            {
              "content": {
                "parts": [{ "text": "{\"result\": \"success\"}" }]
              },
              "finishReason": "STOP"
            }
          ]
        }
        """;
        mockHandler.EnqueueResponse(HttpStatusCode.OK, json);

        using var httpClient = new HttpClient(mockHandler);
        var client = new GeminiLlmClient(httpClient, Options.Create(_defaultOptions));
        var request = new LlmRequest("System", "User");

        // Act
        var response = await client.CompleteAsync(request);

        // Assert
        Assert.Equal("STOP", response.FinishReason);
    }

    [Fact]
    public async Task CompleteAsync_MaxTokensFinishReason_ReturnsSuccessWithFinishReason()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();
        var json = """
        {
          "candidates": [
            {
              "content": {
                "parts": [{ "text": "Truncated content..." }]
              },
              "finishReason": "MAX_TOKENS"
            }
          ]
        }
        """;
        mockHandler.EnqueueResponse(HttpStatusCode.OK, json);

        using var httpClient = new HttpClient(mockHandler);
        var client = new GeminiLlmClient(httpClient, Options.Create(_defaultOptions));
        var request = new LlmRequest("System", "User");

        // Act
        var response = await client.CompleteAsync(request);

        // Assert: MAX_TOKENS must NOT throw in GeminiLlmClient, it returns LlmResponse with FinishReason
        Assert.Equal("MAX_TOKENS", response.FinishReason);
        Assert.Equal("Truncated content...", response.Text);
    }

    [Fact]
    public async Task CompleteAsync_JsonOutputTrue_IncludesResponseMimeTypeInPayload()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();
        string? capturedBody = null;
        mockHandler.EnqueueHandler(async (req, ct) =>
        {
            capturedBody = await req.Content!.ReadAsStringAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                {
                  "candidates": [{
                    "content": { "parts": [{ "text": "{}" }] },
                    "finishReason": "STOP"
                  }]
                }
                """, System.Text.Encoding.UTF8, "application/json")
            };
        });

        using var httpClient = new HttpClient(mockHandler);
        var client = new GeminiLlmClient(httpClient, Options.Create(_defaultOptions));
        var request = new LlmRequest("System", "User", JsonOutput: true);

        // Act
        await client.CompleteAsync(request);

        // Assert
        Assert.NotNull(capturedBody);
        Assert.Contains("\"responseMimeType\":\"application/json\"", capturedBody);
    }

    [Fact]
    public async Task CompleteAsync_JsonOutputFalse_DoesNotIncludeResponseMimeTypeInPayload()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();
        string? capturedBody = null;
        mockHandler.EnqueueHandler(async (req, ct) =>
        {
            capturedBody = await req.Content!.ReadAsStringAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                {
                  "candidates": [{
                    "content": { "parts": [{ "text": "plain text" }] },
                    "finishReason": "STOP"
                  }]
                }
                """, System.Text.Encoding.UTF8, "application/json")
            };
        });

        using var httpClient = new HttpClient(mockHandler);
        var client = new GeminiLlmClient(httpClient, Options.Create(_defaultOptions));
        var request = new LlmRequest("System", "User", JsonOutput: false);

        // Act
        await client.CompleteAsync(request);

        // Assert
        Assert.NotNull(capturedBody);
        Assert.DoesNotContain("responseMimeType", capturedBody);
    }

    [Fact]
    public async Task FakeLlmClient_SupportsFinishReason()
    {
        // Arrange
        var fake = new FakeLlmClient();
        fake.EnqueueResponse("Hello", finishReason: "MAX_TOKENS");

        // Act
        var response = await fake.CompleteAsync(new LlmRequest("sys", "user"));

        // Assert
        Assert.Equal("MAX_TOKENS", response.FinishReason);
    }
}
