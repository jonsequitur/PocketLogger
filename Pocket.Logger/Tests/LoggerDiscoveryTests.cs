using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Example.Instrumented.Library;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using static Pocket.LogEvents;

namespace Pocket.Tests
{
    public class LoggerDiscoveryTests : IDisposable
    {
        private readonly IDisposable disposables;

        public LoggerDiscoveryTests(ITestOutputHelper output)
        {
            disposables = Subscribe(e =>
                                        output.WriteLine(e.ToLogString(), false));
        }

        public void Dispose()
        {
            disposables?.Dispose();
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

        [Fact]
        public void Discovered_loggers_can_be_inspected_via_the_returned_LoggerSubscription()
        {
            using (var subscription = Subscribe(e =>
            {
            }))
            {
                foreach (var type in subscription.DiscoveredLoggerTypes)
                {
                    Logger.Log.Info(type.Assembly.ToString());
                }

                subscription
                    .DiscoveredLoggerTypes
                    .Select(t => t.Assembly)
                    .Should()
                    .BeEquivalentTo(
                        typeof(Class1).Assembly,
                        GetType().Assembly);
            }
        }

        [Fact]
        public void Logger_discovery_can_specify_assemblies_to_search()
        {
            using (var subscription = Subscribe(e =>
            {
            }, new[] { typeof(Class1).Assembly }))
            {
                foreach (var type in subscription.DiscoveredLoggerTypes)
                {
                    Logger.Log.Info(type.Assembly.ToString());
                }

                subscription
                    .DiscoveredLoggerTypes
                    .Select(t => t.Assembly)
                    .Should()
                    .BeEquivalentTo(
                        typeof(Class1).Assembly);
            }
        }
    }
}
