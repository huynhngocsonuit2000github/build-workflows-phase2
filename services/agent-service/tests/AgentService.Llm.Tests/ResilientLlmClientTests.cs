using AgentService.Llm.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgentService.Llm.Tests;

public class ResilientLlmClientTests
{
    private readonly LlmOptions _defaultOptions = new()
    {
        PrimaryModel = "gemini-3.8-flash",
        FallbackModel = "gemini-3.5-flash-lite",
        MaxAttemptsPerModel = 3,
        PerAttemptTimeout = TimeSpan.FromSeconds(5),
        BaseDelay = TimeSpan.FromSeconds(1),
        MaxDelay = TimeSpan.FromSeconds(20),
        EnableJitter = false // deterministic testing of backoff delays
    };

    [Fact]
    public async Task CompleteAsync_ImmediateSuccess_SingleAttempt_UsedFallbackFalse()
    {
        // Arrange
        int callCount = 0;
        var client = new ResilientLlmClient(
            (req, _) =>
            {
                callCount++;
                return Task.FromResult(new LlmResponse("OK", req.ModelOverride ?? "gemini-3.8-flash", false, 1, 10, 20, TimeSpan.FromMilliseconds(5)));
            },
            Options.Create(_defaultOptions));

        var request = new LlmRequest("System", "User");

        // Act
        var response = await client.CompleteAsync(request);

        // Assert
        Assert.Equal(1, callCount);
        Assert.Equal(1, response.Attempts);
        Assert.False(response.UsedFallback);
        Assert.Equal("gemini-3.8-flash", response.ModelUsed);
    }

    [Fact]
    public async Task CompleteAsync_RateLimitedTwiceThenSucceeds_ThreeAttempts_RecordedBackoff()
    {
        // Arrange
        int callCount = 0;
        var recordedDelays = new List<TimeSpan>();

        var client = new ResilientLlmClient(
            (req, _) =>
            {
                callCount++;
                if (callCount < 3)
                {
                    throw new LlmException(
                        "Rate limit reached",
                        LlmErrorKind.RateLimited,
                        httpStatus: 429,
                        retryable: true,
                        model: req.ModelOverride);
                }

                return Task.FromResult(new LlmResponse(
                    "Success after retries",
                    req.ModelOverride ?? "gemini-3.8-flash",
                    false,
                    1,
                    10,
                    20,
                    TimeSpan.FromMilliseconds(5)));
            },
            Options.Create(_defaultOptions),
            delayFunc: (delay, _) =>
            {
                recordedDelays.Add(delay);
                return Task.CompletedTask; // Instant in test
            },
            jitterProvider: () => 0.0);

        var request = new LlmRequest("System", "User");

        // Act
        var response = await client.CompleteAsync(request);

        // Assert
        Assert.Equal(3, callCount);
        Assert.Equal(3, response.Attempts);
        Assert.False(response.UsedFallback);
        Assert.Equal(2, recordedDelays.Count);
        // Exponential backoff with base 1s: attempt 1 -> 1s, attempt 2 -> 2s
        Assert.Equal(TimeSpan.FromSeconds(1), recordedDelays[0]);
        Assert.Equal(TimeSpan.FromSeconds(2), recordedDelays[1]);
    }

    [Fact]
    public async Task CompleteAsync_RateLimitedForever_StopsAfterMaxAttempts_NoInfiniteRetry()
    {
        // Arrange
        int callCount = 0;
        var options = new LlmOptions
        {
            PrimaryModel = "gemini-3.8-flash",
            FallbackModel = null, // fallback disabled
            MaxAttemptsPerModel = 3,
            BaseDelay = TimeSpan.FromMilliseconds(1),
            MaxDelay = TimeSpan.FromMilliseconds(10),
            EnableJitter = false
        };

        var client = new ResilientLlmClient(
            (req, _) =>
            {
                callCount++;
                throw new LlmException(
                    "Rate limit reached",
                    LlmErrorKind.RateLimited,
                    httpStatus: 429,
                    retryable: true,
                    model: req.ModelOverride);
            },
            Options.Create(options),
            delayFunc: (_, _) => Task.CompletedTask);

        var request = new LlmRequest("System", "User");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<LlmException>(() => client.CompleteAsync(request));

        Assert.Equal(3, callCount);
        Assert.Equal(3, ex.Attempts);
        Assert.Equal(LlmErrorKind.RateLimited, ex.Kind);
    }

    [Theory]
    [InlineData(LlmErrorKind.Validation, 400)]
    [InlineData(LlmErrorKind.Unauthorized, 401)]
    [InlineData(LlmErrorKind.EmptyOrBlocked, 200)]
    [InlineData(LlmErrorKind.Permanent, 501)]
    public async Task CompleteAsync_NonRetryableErrors_DoNotRetry_DoNotCallFallback(LlmErrorKind kind, int statusCode)
    {
        // Arrange
        int callCount = 0;
        var client = new ResilientLlmClient(
            (req, _) =>
            {
                callCount++;
                throw new LlmException(
                    $"Error of kind {kind}",
                    kind,
                    httpStatus: statusCode,
                    retryable: false,
                    model: req.ModelOverride);
            },
            Options.Create(_defaultOptions));

        var request = new LlmRequest("System", "User");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<LlmException>(() => client.CompleteAsync(request));

        Assert.Equal(1, callCount);
        Assert.Equal(kind, ex.Kind);
        Assert.Equal(statusCode, ex.HttpStatus);
        Assert.False(ex.Retryable);
    }

    [Fact]
    public async Task CompleteAsync_PrimaryExhaustsWith503_SwitchesToFallback_LogsWarning()
    {
        // Arrange
        int primaryCalls = 0;
        int fallbackCalls = 0;
        var logger = new TestLogger<ResilientLlmClient>();

        var client = new ResilientLlmClient(
            (req, _) =>
            {
                if (req.ModelOverride == "gemini-3.8-flash")
                {
                    primaryCalls++;
                    throw new LlmException(
                        "503 Service Unavailable",
                        LlmErrorKind.Transient,
                        httpStatus: 503,
                        retryable: true,
                        model: req.ModelOverride);
                }

                fallbackCalls++;
                return Task.FromResult(new LlmResponse(
                    "Fallback response",
                    req.ModelOverride!,
                    true,
                    1,
                    15,
                    25,
                    TimeSpan.FromMilliseconds(5)));
            },
            Options.Create(_defaultOptions),
            logger: logger,
            delayFunc: (_, _) => Task.CompletedTask);

        var request = new LlmRequest("System", "User");

        // Act
        var response = await client.CompleteAsync(request);

        // Assert
        Assert.Equal(3, primaryCalls);
        Assert.Equal(1, fallbackCalls);
        Assert.True(response.UsedFallback);
        Assert.Equal("gemini-3.5-flash-lite", response.ModelUsed);
        Assert.Equal(4, response.Attempts); // 3 primary + 1 fallback

        // Verify Warning log was emitted
        var warningLog = logger.Entries.FirstOrDefault(e => e.Level == LogLevel.Warning);
        Assert.NotNull(warningLog);
        Assert.Contains("gemini-3.8-flash", warningLog.Message);
        Assert.Contains("gemini-3.5-flash-lite", warningLog.Message);
        Assert.Contains(nameof(LlmErrorKind.Transient), warningLog.Message);
    }

    [Fact]
    public async Task CompleteAsync_BothPrimaryAndFallbackFail_ThrowsLastExceptionWithCumulativeAttempts()
    {
        // Arrange
        int totalCalls = 0;
        var client = new ResilientLlmClient(
            (req, _) =>
            {
                totalCalls++;
                throw new LlmException(
                    "Service Unavailable",
                    LlmErrorKind.Transient,
                    httpStatus: 503,
                    retryable: true,
                    model: req.ModelOverride);
            },
            Options.Create(_defaultOptions),
            delayFunc: (_, _) => Task.CompletedTask);

        var request = new LlmRequest("System", "User");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<LlmException>(() => client.CompleteAsync(request));

        // 3 on primary + 3 on fallback = 6 total
        Assert.Equal(6, totalCalls);
        Assert.Equal(6, ex.Attempts);
        Assert.Equal(LlmErrorKind.Transient, ex.Kind);
    }

    [Fact]
    public async Task CompleteAsync_FallbackModelEmpty_DoesNotInvokeFallback()
    {
        // Arrange
        int totalCalls = 0;
        var options = new LlmOptions
        {
            PrimaryModel = "gemini-3.8-flash",
            FallbackModel = "", // Empty disables fallback
            MaxAttemptsPerModel = 3
        };

        var client = new ResilientLlmClient(
            (req, _) =>
            {
                totalCalls++;
                throw new LlmException(
                    "Error",
                    LlmErrorKind.Transient,
                    httpStatus: 503,
                    retryable: true,
                    model: req.ModelOverride);
            },
            Options.Create(options),
            delayFunc: (_, _) => Task.CompletedTask);

        var request = new LlmRequest("System", "User");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<LlmException>(() => client.CompleteAsync(request));

        Assert.Equal(3, totalCalls);
        Assert.Equal(3, ex.Attempts);
    }

    [Fact]
    public async Task CompleteAsync_PerAttemptTimeout_TimesOutFirstAttempt_SucceedsOnSecond()
    {
        // Arrange
        int callCount = 0;
        var options = new LlmOptions
        {
            PrimaryModel = "gemini-3.8-flash",
            FallbackModel = "gemini-3.5-flash-lite",
            MaxAttemptsPerModel = 3,
            PerAttemptTimeout = TimeSpan.FromMilliseconds(50),
            BaseDelay = TimeSpan.FromMilliseconds(1),
            MaxDelay = TimeSpan.FromMilliseconds(10),
            EnableJitter = false
        };

        var client = new ResilientLlmClient(
            async (req, ct) =>
            {
                callCount++;
                if (callCount == 1)
                {
                    // First attempt hangs until timeout token fires
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                }

                return new LlmResponse("OK after timeout", req.ModelOverride!, false, 1, 10, 20, TimeSpan.FromMilliseconds(5));
            },
            Options.Create(options),
            delayFunc: (_, _) => Task.CompletedTask);

        var request = new LlmRequest("System", "User");

        // Act
        var response = await client.CompleteAsync(request);

        // Assert
        Assert.Equal(2, callCount);
        Assert.Equal(2, response.Attempts);
        Assert.False(response.UsedFallback);
        Assert.Equal("OK after timeout", response.Text);
    }

    [Fact]
    public async Task CompleteAsync_CallerCancellation_ThrowsOperationCanceledException_NoRetry_NoFallback()
    {
        // Arrange
        int callCount = 0;
        using var cts = new CancellationTokenSource();

        var client = new ResilientLlmClient(
            (req, ct) =>
            {
                callCount++;
                cts.Cancel(); // Simulate caller cancelling mid-request
                ct.ThrowIfCancellationRequested();
                return Task.FromResult(new LlmResponse("OK", req.ModelOverride!, false, 1, 10, 20, TimeSpan.Zero));
            },
            Options.Create(_defaultOptions),
            delayFunc: (_, _) => Task.CompletedTask);

        var request = new LlmRequest("System", "User");

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => client.CompleteAsync(request, cts.Token));

        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task CompleteAsync_RetryAfterLargerThanBackoff_UsesRetryAfterCappedAtMaxDelay()
    {
        // Arrange
        var recordedDelays = new List<TimeSpan>();
        int callCount = 0;

        var options = new LlmOptions
        {
            PrimaryModel = "gemini-3.8-flash",
            MaxAttemptsPerModel = 3,
            BaseDelay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromSeconds(20),
            EnableJitter = false
        };

        var client = new ResilientLlmClient(
            (req, _) =>
            {
                callCount++;
                if (callCount == 1)
                {
                    // Retry-After 10s > backoff 1s => should wait 10s
                    throw new LlmException("Rate limited", LlmErrorKind.RateLimited, 429, true, TimeSpan.FromSeconds(10), req.ModelOverride);
                }
                if (callCount == 2)
                {
                    // Retry-After 50s > MaxDelay 20s => should cap at 20s
                    throw new LlmException("Rate limited", LlmErrorKind.RateLimited, 429, true, TimeSpan.FromSeconds(50), req.ModelOverride);
                }

                return Task.FromResult(new LlmResponse("OK", req.ModelOverride!, false, 1, 10, 20, TimeSpan.Zero));
            },
            Options.Create(options),
            delayFunc: (delay, _) =>
            {
                recordedDelays.Add(delay);
                return Task.CompletedTask;
            },
            jitterProvider: () => 0.0);

        var request = new LlmRequest("System", "User");

        // Act
        var response = await client.CompleteAsync(request);

        // Assert
        Assert.Equal(3, callCount);
        Assert.Equal(2, recordedDelays.Count);
        Assert.Equal(TimeSpan.FromSeconds(10), recordedDelays[0]);
        Assert.Equal(TimeSpan.FromSeconds(20), recordedDelays[1]);
    }

    [Fact]
    public async Task CompleteAsync_ModelOverrideInRequest_UsedInsteadOfPrimaryModel()
    {
        // Arrange
        string? invokedModel = null;
        var client = new ResilientLlmClient(
            (req, _) =>
            {
                invokedModel = req.ModelOverride;
                return Task.FromResult(new LlmResponse("OK", req.ModelOverride!, false, 1, 10, 20, TimeSpan.Zero));
            },
            Options.Create(_defaultOptions));

        var request = new LlmRequest("System", "User", ModelOverride: "custom-experimental-model");

        // Act
        var response = await client.CompleteAsync(request);

        // Assert
        Assert.Equal("custom-experimental-model", invokedModel);
        Assert.Equal("custom-experimental-model", response.ModelUsed);
    }
}
