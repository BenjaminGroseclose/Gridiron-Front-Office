using Microsoft.Extensions.Logging;

namespace GridironFrontOffice.UI.Tests;

/// <summary>Captures log entries so tests can assert what was logged, under which category and event ID.</summary>
internal sealed class ListLoggerProvider : ILoggerProvider
{
    public List<LogEntry> Entries { get; } = [];

    public ILogger CreateLogger(string categoryName) => new ListLogger(categoryName, Entries);

    public void Dispose()
    {
    }

    private sealed class ListLogger(string category, List<LogEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            entries.Add(new LogEntry(category, logLevel, eventId.Id, formatter(state, exception), exception));
    }
}

internal sealed record LogEntry(string Category, LogLevel Level, int EventId, string Message, Exception? Exception);
