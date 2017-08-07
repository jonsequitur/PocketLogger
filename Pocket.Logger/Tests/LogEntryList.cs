using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Pocket.Tests
{
    public class LogEntryList : ConcurrentQueue<(
        byte LogLevel,
        DateTime TimestampUtc,
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
        public void Add(
            (
                byte LogLevel,
                DateTime TimestampUtc,
                Func<(string Message, (string Name, object Value)[] Properties)> Evaluate,
                Exception Exception,
                string OperationName,
                string Category,
                (string Id,
                bool IsStart,
                bool IsEnd,
                bool? IsSuccessful,
                TimeSpan? Duration) Operation) e) =>
            Enqueue(e);

        public (
            byte LogLevel,
            DateTime TimestampUtc,
            Func<(string Message, (string Name, object Value)[] Properties)> Evaluate,
            Exception Exception,
            string OperationName,
            string Category,
            (string Id,
            bool IsStart,
            bool IsEnd,
            bool? IsSuccessful,
            TimeSpan? Duration) Operation) this[int index] =>
            this.ElementAt(index);
    }
}
