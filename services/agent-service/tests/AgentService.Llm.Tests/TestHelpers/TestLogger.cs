using Microsoft.Extensions.Logging;

namespace AgentService.Llm.Tests.TestHelpers;

public sealed record LogEntry(
    LogLevel Level,
    EventId EventId,
    string Message,
    Exception? Exception);

public sealed class TestLogger<T> : ILogger<T>
{
    private readonly List<LogEntry> _entries = new();

    public IReadOnlyList<LogEntry> Entries => _entries;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        _entries.Add(new LogEntry(logLevel, eventId, message, exception));
    }
}
