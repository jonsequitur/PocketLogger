using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Pocket.Tests
{
    public class LogEntryList : ConcurrentQueue<(
        string MessageTemplate,
        object[] Args,
        List<(string Name, object Value)> Properties,
        byte LogLevel,
        DateTime TimestampUtc,
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
                string MessageTemplate,
                object[] Args,
                List<(string Name, object Value)> Properties,
                byte LogLevel,
                DateTime TimestampUtc,
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
            string MessageTemplate,
            object[] Args,
            List<(string Name, object Value)> Properties,
            byte LogLevel,
            DateTime TimestampUtc,
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