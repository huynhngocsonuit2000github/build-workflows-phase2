using System.Net;

namespace AgentService.Llm.Tests.TestHelpers;

public sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> _handlers = new();
    private readonly List<HttpRequestMessage> _sentRequests = new();

    public IReadOnlyList<HttpRequestMessage> SentRequests => _sentRequests;

    public void EnqueueResponse(HttpStatusCode statusCode, string content, string? retryAfterHeader = null)
    {
        _handlers.Enqueue((_, _) =>
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
            };
            if (!string.IsNullOrEmpty(retryAfterHeader))
            {
                response.Headers.TryAddWithoutValidation("Retry-After", retryAfterHeader);
            }
            return Task.FromResult(response);
        });
    }

    public void EnqueueException(Exception exception)
    {
        _handlers.Enqueue((_, _) => Task.FromException<HttpResponseMessage>(exception));
    }

    public void EnqueueHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        _handlers.Enqueue(handler);
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _sentRequests.Add(request);

        if (_handlers.Count > 0)
        {
            var handler = _handlers.Dequeue();
            return handler(request, cancellationToken);
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
        });
    }
}
