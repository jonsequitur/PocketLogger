using System;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using static Pocket.Logger<Pocket.Tests.LoggerUsingStaticLogCategorizationTests>;
using static Pocket.LogEvents;

namespace Pocket.Tests
{
    public class LoggerUsingStaticLogCategorizationTests
    {
        private readonly IDisposable disposables;

        public LoggerUsingStaticLogCategorizationTests(ITestOutputHelper output)
        {
            disposables =
                Subscribe(e =>
                                  output.WriteLine(e.Format()));
        }

        [Fact]
        public void Using_static_can_be_used_to_specify_a_log_category_for_the_whole_file()
        {
            var log = new LogEntryList();

            using (Subscribe(log.Add))
            {
               Log.Info("hi!");
            }

            log.Should()
               .Contain(e => e.Category == typeof(LoggerUsingStaticLogCategorizationTests).FullName);
        }
    }
}
