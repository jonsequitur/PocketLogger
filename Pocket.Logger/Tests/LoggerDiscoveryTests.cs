using System;
using System.Threading.Tasks;
using Example.Instrumented.Library;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using static Pocket.LogEvents;

namespace Pocket.Tests
{
    public class LoggerDiscoveryTests
    {
        private readonly ITestOutputHelper output;

        public LoggerDiscoveryTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public async Task Loggers_in_referenced_assemblies_can_be_discovered_and_subscribed()
        {
            var log = new LogEntryList();

            var message = $"hello from {nameof(Loggers_in_referenced_assemblies_can_be_discovered_and_subscribed)}";

            using (Subscribe(log.Add, discoverOtherPocketLoggers: true))
            {
                Class1.EmitSomeLogEvents(message);
                await Task.Delay(100);
            }

            foreach (var e in log)
            {
                output.WriteLine(e.ToLogString());
            }

            log.Should().Contain(e => e.ToLogString().Contains(message));
        }

        [Fact]
        public async Task Loggers_in_referenced_assemblies_can_be_unsubscribed()
        {
            var log = new LogEntryList();

            using (Subscribe(log.Add, discoverOtherPocketLoggers: true))
            {
                Class1.EmitSomeLogEvents($"before unsubscribe");
                await Task.Delay(100);
            }

            Class1.EmitSomeLogEvents($"after unsubscribe");

            foreach (var e in log)
            {
                output.WriteLine(e.ToLogString());
            }

            log.Should().Contain(e => e.ToLogString().Contains("before unsubscribe"));
            log.Should().NotContain(e => e.ToLogString().Contains("after unsubscribe"));
        }

        [Fact]
        public void Exceptions_thrown_by_subscribers_are_not_thrown_to_the_caller()
        {
            using (Subscribe(_ => throw new Exception("oops!"), discoverOtherPocketLoggers: true))
            {
                Class1.EmitSomeLogEvents();
            }
        }
    }
}
