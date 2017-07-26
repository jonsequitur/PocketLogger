using System;
using FluentAssertions;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using static Pocket.Logger<Pocket.Tests.LogEnrichmentTests>;
using static Pocket.LogEvents;

namespace Pocket.Tests
{
    public class LogEnrichmentTests
    {
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        public LogEnrichmentTests(ITestOutputHelper output)
        {
            disposables.Add(Subscribe(e => output.WriteLine(e.Format())));
        }

        public void Dispose() => disposables.Dispose();

        [Fact]
        public void Log_events_can_be_enriched_prior_to_posting()
        {
            var log = new LogEntryList();

            using (Enrich(add =>
            {
                add(("enriched", "hello!"));
            }))
            using (Subscribe(log.Add))
            using (var operation = Log.OnEnterAndExit())
            {
                operation.Info("one");
                Log.Info("two");
                operation.Event();
            }

            log.Should()
               .OnlyContain(e =>
                                e.Evaluate()
                                 .Properties
                                 .Any(p => p.Name == "enriched" &&
                                           p.Value.Equals("hello!")));
        }

        [Fact]
        public void Enriching_log_events_does_not_hide_normal_properties()
        {
            var log = new LogEntryList();

            using (Enrich(add => add(("enriched-property", "hello!"))))
            using (Subscribe(log.Add))
            using (var operation = Logger.Log.OnEnterAndExit())
            {
                operation.Info("one {one}", 1);
                Logger.Log.Info("two {two}", "two");
                operation.Event(metrics: ("my-metric", 12.3));
            }

            log[1].Evaluate()
                  .Properties
                  .Should()
                  .Contain(p => p.Name == "one" &&
                                p.Value.Equals(1));
            log[2].Evaluate()
                  .Properties
                  .Should()
                  .Contain(p => p.Name == "two" &&
                                p.Value.Equals("two"));
            log[3].Evaluate()
                  .Properties
                  .Select(p => p.Value)
                  .Should()
                  .Contain(("my-metric", 12.3));
        }

        [Fact]
        public void Multiple_enrichers_can_be_in_effect_at_once()
        {
            var log = new LogEntryList();

            using (Subscribe(log.Add))
            using (Enrich(add => add(("enricher-1", 1))))
            {
                Log.Event();

                using (Enrich(add => add(("enricher-2", 3))))
                {
                    Log.Event();
                }
            }

            log[0].Evaluate()
                  .Properties
                  .Should()
                  .Contain(p => p.Name == "enricher-1");

            log[1].Evaluate()
                  .Properties
                  .Should()
                  .Contain(p => p.Name == "enricher-1");
            log[1].Evaluate()
                  .Properties
                  .Should()
                  .Contain(p => p.Name == "enricher-2");
        }

        [Fact]
        public void Enrichers_can_be_removed()
        {
            var log = new LogEntryList();

            using (Subscribe(log.Add))
            using (Enrich(add => add(("enricher-1", 1))))
            {
                using (Enrich(add => add(("enricher-2", 3))))
                {
                }

                Log.Event();
            }

            log[0].Evaluate()
                  .Properties
                  .Should()
                  .Contain(p => p.Name == "enricher-1");
            log[0].Evaluate()
                  .Properties
                  .Should()
                  .NotContain(p => p.Name == "enricher-2");
        }
    }
}
