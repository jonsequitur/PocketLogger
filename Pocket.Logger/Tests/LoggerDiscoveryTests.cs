using System;
using System.Collections.Generic;
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
            var log = new List<IReadOnlyCollection<KeyValuePair<string, object>>>();

            var message = $"hello from {nameof(Loggers_in_referenced_assemblies_can_be_discovered_and_subscribed)}";

            using (Log.DiscoverAndSubscribe(log.Add))
            {
                Class1.EmitSomeLogEvents(message);
                await Task.Delay(100);
            }

            foreach (var e in log)
            {
                output.WriteLine(e.ToString());
            }

            log.Should().Contain(e => e.ToString().Contains(message));
        }

        [Fact]
        public async Task Loggers_in_referenced_assemblies_can_be_unsubscribed()
        {
            var log = new List<IReadOnlyCollection<KeyValuePair<string, object>>>();

            using (Log.DiscoverAndSubscribe(log.Add))
            {
                Class1.EmitSomeLogEvents($"before unsubscribe");
                await Task.Delay(100);
            }

            Class1.EmitSomeLogEvents($"after unsubscribe");

            foreach (var e in log)
            {
                output.WriteLine(e.ToString());
            }

            log.Should().Contain(e => e.ToString().Contains("before unsubscribe"));
            log.Should().NotContain(e => e.ToString().Contains("after unsubscribe"));
        }
    }
}
