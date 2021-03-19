using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Xunit.Abstractions;

#nullable disable

namespace Pocket.For.Xunit
{
    internal class TestLog : IDisposable
    {
        private static readonly AsyncLocal<TestLog> _current = new();

        private readonly CompositeDisposable _disposables;

        private static readonly object _lockObj = new();

        public TestLog(
            MethodInfo testMethod,
            bool writeToFile = false,
            string filename = null)
        {
            if (testMethod == null)
            {
                throw new ArgumentNullException(nameof(testMethod));
            }

            var testName = $"{testMethod.DeclaringType.Name}.{testMethod.Name}";

            _disposables = new()
            {
                () => Log.Dispose()
            };

            if (writeToFile)
            {
                filename ??= $"{testName}-{DateTime.Now:yyyy-MM-dd-hh-mm-ss}.log";
                LogFile = new FileInfo(filename);

                LogToFile();
            }

            TestName = testName;

            Log = new OperationLogger($"🧪:{TestName}", logOnStart: true);
        }

        private void LogToFile()
        {
            _disposables.Add(
                LogEvents.Subscribe(e =>
                {
                    var entry = e.ToLogString() + Environment.NewLine;

                    lock (_lockObj)
                    {
                        File.AppendAllText(LogFile.FullName, entry);
                    }
                }));
        }

        public OperationLogger Log { get; }

        public FileInfo LogFile { get; }

        public string TestName { get; }

        public IEnumerable<string> Lines
        {
            get
            {
                if (LogFile is { })
                {
                    lock (_lockObj)
                    {
                        using var streamReader = new StreamReader(LogFile.OpenRead());
                        return streamReader.ReadToEnd().Trim().Split(new[] { '\n', '\r' });
                    }
                }
                else
                {
                    throw new InvalidOperationException("Logging to file must be enabled in order to read back log content using this method.");
                }
            }
        }

        public static TestLog Current
        {
            get => _current.Value;
            set => _current.Value = value;
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        public void LogTo(ITestOutputHelper output)
        {
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            _disposables.Add(LogEvents.Subscribe(e => output.WriteLine(e.ToLogString())));
        }
    }
}