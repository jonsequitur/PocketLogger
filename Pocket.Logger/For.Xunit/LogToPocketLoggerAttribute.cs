using System;
using System.Collections.Concurrent;
using System.Reflection;
using Xunit.Sdk;

#nullable disable

namespace Pocket.For.Xunit
{
    internal class LogToPocketLoggerAttribute : BeforeAfterTestAttribute
    {
        private readonly string filename;

        private readonly bool writeToFile;

        private static readonly ConcurrentDictionary<MethodInfo, TestLog> _operations = new();

        public LogToPocketLoggerAttribute(bool writeToFile = false)
        {
            this.writeToFile = writeToFile;
        }

        public LogToPocketLoggerAttribute(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(filename));
            }

            this.filename = filename;
            writeToFile = true;
        }

        public override void Before(MethodInfo methodUnderTest)
        {
            var testLog = new TestLog(
                methodUnderTest,
                writeToFile,
                filename);

            TestLog.Current = testLog;

            _operations.TryAdd(
                methodUnderTest,
                testLog);
        }

        public override void After(MethodInfo methodUnderTest)
        {
            if (_operations.TryRemove(methodUnderTest, out var testLog))
            {
                testLog.Dispose();

                if (TestLog.Current == testLog)
                {
                    TestLog.Current = default;
                }
            }

            base.After(methodUnderTest);
        }
    }
}