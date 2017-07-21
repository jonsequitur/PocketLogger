using System;
using System.Threading.Tasks;
using Example.Instrumented.Library;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

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

            using (Log.Subscribe(log.Add, discoverOtherPocketLoggers: true))
            {
                Class1.EmitSomeLogEvents(message);
                await Task.Delay(100);
            }

            foreach (var e in log)
            {
                output.WriteLine(e.Format());
            }

            log.Should().Contain(e => e.Format().Contains(message));
        }

        [Fact]
        public async Task Loggers_in_referenced_assemblies_can_be_unsubscribed()
        {
            var log = new LogEntryList();

            using (Log.Subscribe(log.Add, discoverOtherPocketLoggers: true))
            {
                Class1.EmitSomeLogEvents($"before unsubscribe");
                await Task.Delay(100);
            }

            Class1.EmitSomeLogEvents($"after unsubscribe");

            foreach (var e in log)
            {
                output.WriteLine(e.Format());
            }

            log.Should().Contain(e => e.Format().Contains("before unsubscribe"));
            log.Should().NotContain(e => e.Format().Contains("after unsubscribe"));
        }

        [Fact]
        public void Exceptions_thrown_by_subscribers_are_not_thrown_to_the_caller()
        {
            using (Log.Subscribe(_ => throw new Exception("oops!"), discoverOtherPocketLoggers: true))
            {
                Class1.EmitSomeLogEvents();
            }
        }
    }
}
