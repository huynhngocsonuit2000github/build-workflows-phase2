using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgentService.Llm;

/// <summary>
/// Resilient LLM client wrapper providing per-attempt timeout, exponential backoff retries,
/// and fallback model failover.
/// </summary>
public class ResilientLlmClient : ILlmClient
{
    private readonly Func<LlmRequest, CancellationToken, Task<LlmResponse>> _modelInvoker;
    private readonly LlmOptions _options;
    private readonly ILogger<ResilientLlmClient>? _logger;
    private readonly TimeProvider _timeProvider;
    private readonly Func<TimeSpan, CancellationToken, Task> _delayFunc;
    private readonly Func<double>? _jitterProvider;

    public ResilientLlmClient(
        ILlmClient innerClient,
        IOptions<LlmOptions> options,
        ILogger<ResilientLlmClient>? logger = null,
        TimeProvider? timeProvider = null,
        Func<TimeSpan, CancellationToken, Task>? delayFunc = null,
        Func<double>? jitterProvider = null)
        : this(
            (innerClient ?? throw new ArgumentNullException(nameof(innerClient))).CompleteAsync,
            options,
            logger,
            timeProvider,
            delayFunc,
            jitterProvider)
    {
    }

    public ResilientLlmClient(
        Func<LlmRequest, CancellationToken, Task<LlmResponse>> modelInvoker,
        IOptions<LlmOptions> options,
        ILogger<ResilientLlmClient>? logger = null,
        TimeProvider? timeProvider = null,
        Func<TimeSpan, CancellationToken, Task>? delayFunc = null,
        Func<double>? jitterProvider = null)
    {
        _modelInvoker = modelInvoker ?? throw new ArgumentNullException(nameof(modelInvoker));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _delayFunc = delayFunc ?? ((delay, ct) => Task.Delay(delay, _timeProvider, ct));
        _jitterProvider = jitterProvider;
    }

    public async Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ct.ThrowIfCancellationRequested();

        var primaryModel = !string.IsNullOrWhiteSpace(request.ModelOverride)
            ? request.ModelOverride
            : (!string.IsNullOrWhiteSpace(_options.PrimaryModel) ? _options.PrimaryModel : "gemini-3.8-flash");

        var fallbackModel = _options.FallbackModel;
        var canFallback = !string.IsNullOrWhiteSpace(fallbackModel) &&
                          !string.Equals(fallbackModel, primaryModel, StringComparison.OrdinalIgnoreCase);

        var startTimestamp = _timeProvider.GetTimestamp();
        int totalAttempts = 0;

        // 1. Attempt primary model
        var (primarySuccess, primaryResponse, primaryLastEx) = await ExecuteAttemptsForModelAsync(
            request,
            primaryModel,
            usedFallback: false,
            initialAttemptOffset: totalAttempts,
            ct: ct).ConfigureAwait(false);

        if (primarySuccess)
        {
            var totalLatency = _timeProvider.GetElapsedTime(startTimestamp);
            var result = primaryResponse! with { Latency = totalLatency };

            _logger?.LogInformation(
                "LLM call succeeded using model '{ModelUsed}' (fallback={UsedFallback}, attempts={Attempts}, latency={LatencyMs}ms).",
                result.ModelUsed,
                result.UsedFallback,
                result.Attempts,
                totalLatency.TotalMilliseconds);

            return result;
        }

        totalAttempts += _options.MaxAttemptsPerModel;

        // If fallback is not configured or disabled, throw the last primary exception immediately
        if (!canFallback)
        {
            _logger?.LogError(
                "Primary model '{PrimaryModel}' failed after {Attempts} attempts. No fallback configured. Error: {ErrorKind}.",
                primaryModel,
                totalAttempts,
                primaryLastEx!.Kind);

            throw primaryLastEx!;
        }

        // 2. Switching to fallback model
        _logger?.LogWarning(
            "Primary model '{PrimaryModel}' failed after {Attempts} attempts (last error: {ErrorKind}). Switching to fallback model '{FallbackModel}'.",
            primaryModel,
            _options.MaxAttemptsPerModel,
            primaryLastEx!.Kind,
            fallbackModel);

        var (fallbackSuccess, fallbackResponse, fallbackLastEx) = await ExecuteAttemptsForModelAsync(
            request,
            fallbackModel!,
            usedFallback: true,
            initialAttemptOffset: totalAttempts,
            ct: ct).ConfigureAwait(false);

        if (fallbackSuccess)
        {
            var totalLatency = _timeProvider.GetElapsedTime(startTimestamp);
            var result = fallbackResponse! with { Latency = totalLatency };

            _logger?.LogInformation(
                "LLM call succeeded using fallback model '{ModelUsed}' (attempts={Attempts}, latency={LatencyMs}ms).",
                result.ModelUsed,
                result.Attempts,
                totalLatency.TotalMilliseconds);

            return result;
        }

        totalAttempts += _options.MaxAttemptsPerModel;

        _logger?.LogError(
            "Both primary '{PrimaryModel}' and fallback '{FallbackModel}' failed after {TotalAttempts} total attempts. Last error: {ErrorKind}.",
            primaryModel,
            fallbackModel,
            totalAttempts,
            fallbackLastEx!.Kind);

        // Throw last error from fallback, carrying the cumulative attempts
        throw new LlmException(
            $"All attempts failed across models '{primaryModel}' and '{fallbackModel}'. Last error: {fallbackLastEx!.Message}",
            fallbackLastEx.Kind,
            httpStatus: fallbackLastEx.HttpStatus,
            retryable: fallbackLastEx.Retryable,
            retryAfter: fallbackLastEx.RetryAfter,
            model: fallbackModel,
            attempts: totalAttempts,
            innerException: fallbackLastEx);
    }

    private async Task<(bool Success, LlmResponse? Response, LlmException? LastException)> ExecuteAttemptsForModelAsync(
        LlmRequest request,
        string model,
        bool usedFallback,
        int initialAttemptOffset,
        CancellationToken ct)
    {
        LlmException? lastException = null;
        var modelRequest = request with { ModelOverride = model };

        for (int attempt = 1; attempt <= _options.MaxAttemptsPerModel; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            var cumulativeAttempts = initialAttemptOffset + attempt;

            try
            {
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                using var timer = _timeProvider.CreateTimer(
                    _ =>
                    {
                        try { linkedCts.Cancel(); } catch (ObjectDisposedException) { }
                    },
                    null,
                    _options.PerAttemptTimeout,
                    Timeout.InfiniteTimeSpan);

                LlmResponse response;
                try
                {
                    var task = _modelInvoker(modelRequest, linkedCts.Token);
                    response = await task.WaitAsync(_options.PerAttemptTimeout, _timeProvider, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    // Caller cancellation: never catch, never wrap, throw immediately
                    throw;
                }
                catch (OperationCanceledException ex)
                {
                    // Per-attempt timeout occurred via linkedCts
                    throw new LlmException(
                        $"Invocation of model '{model}' timed out after {_options.PerAttemptTimeout}.",
                        LlmErrorKind.Timeout,
                        httpStatus: null,
                        retryable: true,
                        model: model,
                        attempts: cumulativeAttempts,
                        innerException: ex);
                }
                catch (TimeoutException ex)
                {
                    // Per-attempt timeout occurred via WaitAsync
                    throw new LlmException(
                        $"Invocation of model '{model}' timed out after {_options.PerAttemptTimeout}.",
                        LlmErrorKind.Timeout,
                        httpStatus: null,
                        retryable: true,
                        model: model,
                        attempts: cumulativeAttempts,
                        innerException: ex);
                }

                return (true, response with
                {
                    ModelUsed = model,
                    UsedFallback = usedFallback,
                    Attempts = cumulativeAttempts
                }, null);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Caller cancellation: do not retry, propagate immediately
                throw;
            }
            catch (LlmException ex)
            {
                lastException = new LlmException(
                    ex.Message,
                    ex.Kind,
                    httpStatus: ex.HttpStatus,
                    retryable: ex.Retryable,
                    retryAfter: ex.RetryAfter,
                    model: model,
                    attempts: cumulativeAttempts,
                    innerException: ex.InnerException);

                // Non-retryable error: throw immediately without further retries or fallback
                if (!ex.Retryable)
                {
                    throw lastException;
                }

                // If attempts remain for this model, delay with backoff
                if (attempt < _options.MaxAttemptsPerModel)
                {
                    var delay = CalculateDelay(attempt, ex.RetryAfter);
                    await _delayFunc(delay, ct).ConfigureAwait(false);
                }
            }
        }

        return (false, null, lastException);
    }

    private TimeSpan CalculateDelay(int attempt, TimeSpan? retryAfter)
    {
        // delay = min(MaxDelay, BaseDelay * 2^(n-1)) + jitter
        var exponentialFactor = Math.Pow(2, attempt - 1);
        var baseDelayMs = _options.BaseDelay.TotalMilliseconds * exponentialFactor;
        var cappedBackoffMs = Math.Min(_options.MaxDelay.TotalMilliseconds, baseDelayMs);

        double jitterMs = 0;
        if (_options.EnableJitter)
        {
            if (_jitterProvider != null)
            {
                jitterMs = _jitterProvider() * 100.0;
            }
            else
            {
                // Small jitter up to 10% of backoff or 100ms
                var maxJitter = Math.Min(100.0, cappedBackoffMs * 0.1);
                jitterMs = Random.Shared.NextDouble() * (maxJitter > 0 ? maxJitter : 10.0);
            }
        }

        var delay = TimeSpan.FromMilliseconds(cappedBackoffMs + jitterMs);
        if (delay > _options.MaxDelay)
        {
            delay = _options.MaxDelay;
        }

        // If Retry-After is specified, use the larger of the two, capped at MaxDelay
        if (retryAfter.HasValue)
        {
            if (retryAfter.Value > delay)
            {
                delay = retryAfter.Value;
            }
            if (delay > _options.MaxDelay)
            {
                delay = _options.MaxDelay;
            }
        }

        return delay;
    }
}
