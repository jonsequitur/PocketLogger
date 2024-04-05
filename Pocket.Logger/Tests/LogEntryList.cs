#nullable enable

using System.Collections.Concurrent;
using System.Linq;
using LogEvent = (
    string MessageTemplate,
    object[]? Args, System.Collections.Generic.List<(string Name, object Value)> Properties,
    byte LogLevel,
    System.DateTime TimestampUtc,
    System.Exception? Exception,
    string? OperationName,
    string? Category,
    (string? Id,
    bool IsStart,
    bool IsEnd,
    bool? IsSuccessful,
    System.TimeSpan? Duration) Operation);

namespace Pocket.Tests;

public class LogEntryList : ConcurrentQueue<LogEvent>
{
    public void Add(LogEvent e) => Enqueue(e);

    public LogEvent this[int index] => this.ElementAt(index);
}