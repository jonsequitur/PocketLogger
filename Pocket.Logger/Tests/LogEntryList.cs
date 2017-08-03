using System;
using System.Collections.Generic;

namespace Pocket.Tests
{
    public class LogEntryList : List<(
        byte LogLevel,
        DateTimeOffset Timestamp,
        Func<(string Message, (string Name, object Value)[] Properties)> Evaluate,
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
