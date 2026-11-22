namespace AgentService.Llm;

/// <summary>
/// Test double for ILlmClient that records requests and returns pre-programmed responses or errors.
/// </summary>
public sealed class FakeLlmClient : ILlmClient
{
    private readonly Queue<Func<LlmRequest, Task<LlmResponse>>> _handlers = new();
    private readonly List<LlmRequest> _receivedRequests = new();

    public IReadOnlyList<LlmRequest> ReceivedRequests => _receivedRequests;

    public FakeLlmClient()
    {
    }

    public FakeLlmClient(string defaultResponseText)
    {
        EnqueueResponse(defaultResponseText);
    }

    public void EnqueueResponse(
        string text,
        string? model = null,
        int? inputTokens = null,
        int? outputTokens = null,
        TimeSpan? latency = null)
    {
        _handlers.Enqueue(req => Task.FromResult(new LlmResponse(
            Text: text,
            ModelUsed: model ?? req.ModelOverride ?? "fake-model",
            UsedFallback: false,
            Attempts: 1,
            InputTokens: inputTokens ?? 10,
            OutputTokens: outputTokens ?? 20,
            Latency: latency ?? TimeSpan.FromMilliseconds(5))));
    }

    public void EnqueueException(Exception exception)
    {
        _handlers.Enqueue(_ => Task.FromException<LlmResponse>(exception));
    }

    public void EnqueueHandler(Func<LlmRequest, Task<LlmResponse>> handler)
    {
        _handlers.Enqueue(handler);
    }

    public Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ct.ThrowIfCancellationRequested();

        _receivedRequests.Add(request);

        if (_handlers.Count > 0)
        {
            var handler = _handlers.Dequeue();
            return handler(request);
        }

        return Task.FromResult(new LlmResponse(
            Text: "Default fake response",
            ModelUsed: request.ModelOverride ?? "fake-model",
            UsedFallback: false,
            Attempts: 1,
            InputTokens: 10,
            OutputTokens: 20,
            Latency: TimeSpan.FromMilliseconds(5)));
    }
}
