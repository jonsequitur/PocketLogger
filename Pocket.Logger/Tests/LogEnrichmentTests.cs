using System;
using FluentAssertions;
using System.Linq;
using Example.Instrumented.Library;
using Xunit;
using Xunit.Abstractions;
using static Pocket.Logger<Pocket.Tests.LogEnrichmentTests>;
using static Pocket.LogEvents;

namespace Pocket.Tests;

public class LogEnrichmentTests : IDisposable
{
    private readonly CompositeDisposable disposables = new CompositeDisposable();

    public LogEnrichmentTests(ITestOutputHelper output)
    {
        disposables.Add(Subscribe(e => output.WriteLine(e.ToLogString())));
    }

    public void Dispose() => disposables.Dispose();

    [Fact]
    public void Log_events_can_be_enriched_prior_to_posting()
    {
        var log = new LogEntryList();

        using (Subscribe(log.Add))
        using (Enrich(add => add(("enriched", "hello!"))))
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
    public void Enrichers_affect_events_from_the_current_assembly()
    {
        var log = new LogEntryList();

        using (Subscribe(log.Add,
                         new[] { typeof(Class1).Assembly, GetType().Assembly }))
        using (Enrich(add =>
               {
                   add(("enriched", "with extra stuff"));
               }))
        {
            using var operation = Logger.Log.OnEnterAndExit();
                
            operation.Info("hello!");
        }

        log.Count
           .Should()
           .Be(3);

        foreach (var e in log)
        {
            e.Evaluate()
             .Properties
             .Should()
             .ContainSingle(t => Equals(t.Name, "enriched") &&
                                 Equals(t.Value, "with extra stuff"));
        }
    }

    [Fact]
    public void Enrichers_affect_events_from_another_assembly()
    {
        var log = new LogEntryList();
        var assembliesToSearch = new[] { typeof(Class1).Assembly, GetType().Assembly };
        using (Subscribe(log.Add, assembliesToSearch))
        using (Enrich(add =>
               {
                   add(("enriched", "with extra stuff"));
               }, assembliesToSearch))
        {
            Class1.EmitSomeLogEvents("hello!");
        }

        log.Count
           .Should()
           .Be(3);

        for (var i = 0; i < log.Count; i++)
        {
            var e = log[i];
            e.Evaluate()
             .Properties
             .Should()
             .ContainSingle(t => Equals(t.Name, "enriched") &&
                                 Equals(t.Value, "with extra stuff"));
        }
    }

    [Fact]
    public void Enriching_log_events_does_not_hide_normal_properties()
    {
        var log = new LogEntryList();

        using (Subscribe(log.Add))
        using (Enrich(add => add(("enriched-property", "hello!"))))
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
    public void Enrichers_affect_all_active_subscriptions()
    {
        var outerSubscription = new LogEntryList();
        var innerSubscription = new LogEntryList();

        using (Subscribe(outerSubscription.Add))
        using (Enrich(add => add(("enricher-1", 1))))
        {
            Log.Event();

            using (Subscribe(innerSubscription.Add))
            using (Enrich(add => add(("enricher-2", 3))))
            {
                Log.Event();
            }
        }

        outerSubscription[0].Evaluate()
                            .Properties
                            .Should()
                            .OnlyContain(p => p.Name == "enricher-1");

        outerSubscription[1].Evaluate()
                            .Properties
                            .Should()
                            .Contain(p => p.Name == "enricher-1")
                            .And
                            .Contain(p => p.Name == "enricher-2");

        innerSubscription[0].Evaluate()
                            .Properties
                            .Should()
                            .Contain(p => p.Name == "enricher-1")
                            .And
                            .Contain(p => p.Name == "enricher-2");
    }
}