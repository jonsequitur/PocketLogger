using System;
using System.Reflection;
using Xunit.Sdk;

namespace Pocket.For.Xunit
{
    internal class LogToPocketLoggerAttribute : BeforeAfterTestAttribute
    {
        private readonly string filename;

        private readonly bool writeToFile;

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
            TestLog.Current = new TestLog(
                methodUnderTest,
                writeToFile,
                filename);
        }

        public override void After(MethodInfo methodUnderTest)
        {
            base.After(methodUnderTest);

            TestLog.Current.Dispose();
        }
    }
}
