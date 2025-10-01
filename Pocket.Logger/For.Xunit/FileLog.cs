#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using LogEvent = (string MessageTemplate, object[]? Args, System.Collections.Generic.List<(string Name, object? Value)> Properties, byte LogLevel, System.DateTime TimestampUtc, System.Exception? Exception, string? OperationName, string? Category, (string? Id, bool IsStart, bool IsEnd, bool? IsSuccessful, System.TimeSpan? Duration) Operation);

namespace Pocket.For.Xunit;

internal class FileLog : IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    private static readonly Lock _lockObj = new();

    public FileLog(string filename)
    {
        File = new FileInfo(filename);
        SubscribeFileLog();
    }

    public void Dispose() => _disposables.Dispose();

    private void SubscribeFileLog() => _disposables.Add(LogEvents.Subscribe(WriteLogEntry));

    public FileInfo File { get; }

    public IEnumerable<string> Lines
    {
        get
        {
            lock (_lockObj)
            {
                using var streamReader = new StreamReader(File.OpenRead());
                return streamReader.ReadToEnd().Trim().Split(['\n', '\r']);
            }
        }
    }

    public void Subscribe(Assembly[] assemblies) => _disposables.Add(LogEvents.Subscribe(WriteLogEntry,assemblies));

    private void WriteLogEntry(LogEvent e)
    {
        var entry = e.ToLogString() + Environment.NewLine;

        lock (_lockObj)
        {
            System.IO.File.AppendAllText(File.FullName, entry);
        }
    }
}