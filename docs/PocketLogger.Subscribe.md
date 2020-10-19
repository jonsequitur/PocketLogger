# How to use PocketLogger.Subscribe

The PocketLogger.Subscribe package provides methods for subscribing to log events emitted using the [PocketLogger code instrumentation APIs](./readme.md). It's not opinionated about what you do with the received events, which allows you to easily build adapaters to your output of choice.

A simple example of this is to send output to the console.

``` csharp --source-file ./demo/Examples.cs --project ./demo/demo.csproj --region SubscribeAndSendToConsole --session SubscribeAndSendToConsole
Log.Info("Before subscribing.");

var subscription =
    LogEvents.Subscribe(e => Console.WriteLine(e.ToLogString()));

Log.Info("After subscribing.");

subscription.Dispose();

Log.Info("After disposing the subscription.");
```

``` console --session SubscribeAndSendToConsole
2020-10-19T14:48:13.2776783Z [Demo.Examples]  ℹ After subscribing. 

```

`LogEvents.Subscribe` returns an `IDisposable` that, when disposed, stops observing the log events being published. What you do with the log events received by your handler is up to you. The `ToLogString` method is a convenience for writing them out as text. In many cases though you'll want specific details of the log event so that you can call an API like Application Insights, filter for certain kinds of events, and so on. This example shows some of the details available on the log event `ValueTuple`

``` csharp --source-file ./demo/Examples.cs --project ./demo/demo.csproj --region LogEventStructure --session LogEventStructure
using var subscription =
                LogEvents.Subscribe(e =>
                {
                    Console.WriteLine(e.ToLogString());
                    Console.WriteLine($"    {nameof(e.TimestampUtc)}: {e.TimestampUtc}");
                    Console.WriteLine($"    {nameof(e.Operation.Id)}: {e.Operation.Id}");
                    Console.WriteLine($"    {nameof(e.Category)}: {e.Category}");
                    Console.WriteLine($"    {nameof(e.OperationName)}: {e.OperationName}");
                    Console.WriteLine($"    {nameof(e.LogLevel)}: {e.LogLevel}");
                    Console.WriteLine($"    {nameof(e.Operation.IsStart)}: {e.Operation.IsStart}");
                    Console.WriteLine($"    {nameof(e.Operation.IsEnd)}: {e.Operation.IsEnd}");
                    Console.WriteLine($"    {nameof(e.Operation.Duration)}: {e.Operation.Duration}");
                    Console.WriteLine($"    {nameof(e.Operation.IsSuccessful)}: {e.Operation.IsSuccessful}");
                });

Log.Info("INFO");

            using var operation = Log.OnEnterAndConfirmOnExit();

operation.Event("EVENT");

operation.Succeed();
```

``` console --session LogEventStructure
2020-10-19T14:48:15.3279640Z [Demo.Examples]  ℹ INFO 
    TimestampUtc: 10/19/2020 2:48:15 PM
    Id: 
    Category: Demo.Examples
    OperationName: 
    LogLevel: 3
    IsStart: False
    IsEnd: False
    Duration: 
    IsSuccessful: 
2020-10-19T14:48:15.3593962Z [00-4cdd04ce46acb64e9b1a3cab5798109a-7cdc7ac6b7b29348-00] [Demo.Examples] [LogEventStructure]  ▶  
    TimestampUtc: 10/19/2020 2:48:15 PM
    Id: 00-4cdd04ce46acb64e9b1a3cab5798109a-7cdc7ac6b7b29348-00
    Category: Demo.Examples
    OperationName: LogEventStructure
    LogLevel: 3
    IsStart: True
    IsEnd: False
    Duration: 00:00:00.0018644
    IsSuccessful: 
2020-10-19T14:48:15.3949447Z [00-4cdd04ce46acb64e9b1a3cab5798109a-7cdc7ac6b7b29348-00] [Demo.Examples] [EVENT]  📊 (37.2326ms)  
    TimestampUtc: 10/19/2020 2:48:15 PM
    Id: 00-4cdd04ce46acb64e9b1a3cab5798109a-7cdc7ac6b7b29348-00
    Category: Demo.Examples
    OperationName: EVENT
    LogLevel: 0
    IsStart: False
    IsEnd: False
    Duration: 00:00:00.0372326
    IsSuccessful: 
2020-10-19T14:48:15.3968998Z [00-4cdd04ce46acb64e9b1a3cab5798109a-7cdc7ac6b7b29348-00] [Demo.Examples] [LogEventStructure]  ⏹ -> ✔ (39.1876ms)  
    TimestampUtc: 10/19/2020 2:48:15 PM
    Id: 00-4cdd04ce46acb64e9b1a3cab5798109a-7cdc7ac6b7b29348-00
    Category: Demo.Examples
    OperationName: LogEventStructure
    LogLevel: 3
    IsStart: False
    IsEnd: True
    Duration: 00:00:00.0391876
    IsSuccessful: True

```

## Evaluate

Some of the information carried by a log event isn't capture until `Evaluate` is called. This includes the message (which requires string interpolation) and the building of the dictionary of properties and metrics (based on the message string tokens). Once evaluated, these values are cached, but PocketLogger doesn't do this up front for performance reasons, since you might not be writing or transmitting all of the log events. 

``` csharp --source-file ./demo/Examples.cs --project ./demo/demo.csproj --region Evaluate --session Evaluate
using var subscription =
                LogEvents.Subscribe(e =>
                {
                    var evaluated = e.Evaluate();

                    Console.WriteLine(evaluated.Message);

                    foreach (var property in evaluated.Properties)
                    {
                        Console.WriteLine($"    {property.Name}: {property.Value}");
                    }
                });

Log.Event("EventWithProperties", ("a number", 1), ("a date", DateTime.Now));

```

``` console --session Evaluate
 +[ (a number, 1), (a date, 10/19/2020 7:48:16 AM) ]
    a number: 1
    a date: 10/19/2020 7:48:16 AM

```

