#nullable enable

using System;
using System.Collections.Generic;
using System.IO;

namespace Pocket.For.Xunit;

internal class FileLog
{
    private readonly CompositeDisposable _disposables = new();

    private static readonly object _lockObj = new();

    public FileLog(string filename)
    {
        File = new FileInfo(filename);
        SubscribeFileLog();
    }

    private void SubscribeFileLog()
    {
        _disposables.Add(
            LogEvents.Subscribe(e =>
            {
                var entry = e.ToLogString() + Environment.NewLine;

                lock (_lockObj)
                {
                    System.IO.File.AppendAllText(File.FullName, entry);
                }
            }));
    }

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
}