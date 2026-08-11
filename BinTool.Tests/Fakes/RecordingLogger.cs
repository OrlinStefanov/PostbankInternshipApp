using Microsoft.Extensions.Logging;

namespace BinTool.Tests.Fakes;

// Captures what a service logged, so a test can assert on the level and the event rather than only
// on the value returned. That matters for one thing in particular: a refused write must log at
// Warning, never Error - a guard doing its job is not a failure, and if refusals were logged as
// errors whoever watches the dashboard would learn to ignore the level that means something is
// broken.
public sealed class RecordingLogger<T> : ILogger<T>
{
    public List<LogEntry> Entries { get; } = new();

    public List<object?> Scopes { get; } = new();

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        Scopes.Add(state);
        return new Scope();
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        Entries.Add(new LogEntry(
            logLevel, eventId, formatter(state, exception), exception, Fields(state)));

    // The message template's named holes, which is what a structured sink stores. A test asserting
    // on these is asserting the field is really a field, not text that happens to read like one.
    private static IReadOnlyDictionary<string, object?> Fields<TState>(TState state) =>
        state is IEnumerable<KeyValuePair<string, object?>> pairs
            ? pairs.GroupBy(p => p.Key).ToDictionary(g => g.Key, g => g.Last().Value)
            : new Dictionary<string, object?>();

    public sealed record LogEntry(
        LogLevel Level,
        EventId EventId,
        string Message,
        Exception? Exception,
        IReadOnlyDictionary<string, object?> Fields);

    private sealed class Scope : IDisposable
    {
        public void Dispose() { }
    }
}
