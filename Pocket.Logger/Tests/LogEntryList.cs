using System;
using System.Collections.Generic;

namespace Pocket.Tests
{
    public class LogEntryList : List<(
        int LogLevel,
        DateTimeOffset Timestamp,
        Func<(string Message, IReadOnlyCollection<KeyValuePair<string, object>> Properties)> Evaluate,
        Exception Exception,
        string OperationName,
        string Category,
        (string Id,
        bool IsStart,
        bool IsEnd,
        bool? IsSuccessful,
        TimeSpan? Duration) Operation)>
    {
    }
}
