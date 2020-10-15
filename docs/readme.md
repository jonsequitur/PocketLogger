# How to use PocketLogger

## The Basics

Let's start with the most basic log statements.

``` csharp --source-file ./demo/Examples.cs --project ./demo/demo.csproj --region HelloPocketLogger --session HelloPocketLogger
Log.Info("Hello!");
Log.Event("LoudNoise");
Log.Warning("That's strange...");
Log.Error("Oh no! Run!");
```

``` console --session HelloPocketLogger
2020-10-15T02:03:33.7855884Z [Demo.Examples]  ℹ Hello! 
2020-10-15T02:03:33.7960975Z [Demo.Examples] [LoudNoise]  📊  
2020-10-15T02:03:33.8149299Z [Demo.Examples]  ⚠ That's strange... 
2020-10-15T02:03:33.8150887Z [Demo.Examples]  ❌ Oh no! Run! 

```

So far, the API is what you'd expect.

It's worth pointing out that in addition to the time stamps and the specified message strings, these basic log statements also capture the name and namespace of the containing class. This is enabled by adding a `using static` at the top of the file, like this:

``` csharp --editable false --region UsefulUsings --source-file ./demo/Examples.cs --project ./demo/demo.csproj
using static Pocket.Logger<Demo.Examples>;
```

Note that if the containing class is `static`, this approach won't compile, so you can do the following instead to provide a textually-equivalent API:

``` csharp --source-file ./demo/StaticClassExample.cs --project ./demo/demo.csproj --region LogApiStaticClass --session LogApiStaticClass
public static class StaticClassExample
{
    private static readonly Logger Log = new Logger(nameof(StaticClassExample));

    public static void Hello()
    {
        Log.Info("Hello!");
    }
}
```

``` console --session LogApiStaticClass
2020-10-15T02:03:35.0737980Z [StaticClassExample]  ℹ Hello! 

```

Don't worry for now how the output from these calls makes it to the console. We'll cover that under [`LogEvents.Subscribe`](./PocketLogger.Subscribe.md).

Let's take a closer look first at each of these `Log` methods.

### `Log.Info`

`Log.Info` is used for logging, uh, info. You can pass arbitrary parameters to it and it will include them in the output. Info!

``` csharp --source-file ./demo/Examples.cs --project ./demo/demo.csproj --region LogInfo --session LogInfo
var x = "world";
var y = DateTime.Now;

Log.Info("Hello", x, y);
```

``` console --session LogInfo
2020-10-15T02:03:36.3685833Z [Demo.Examples]  ℹ Hello +[ world, 10/14/2020 7:03:36 PM ] 

```

Like other structured logging APIs, the message can include tokens specifying the names for the values you're passing. If you specify `{` delimited `}` tokens in the message string, then the additional parameters you pass will be interpolated into the message instead of set off to the side in `+[` square brackets `]`.

``` csharp --source-file ./demo/Examples.cs --project ./demo/demo.csproj --region LogInfoWithNamedTemplateParams --session LogInfoWithNamedTemplateParams
var x = "world";
var y = DateTime.Now;

Log.Info("Hello {x}! The time is {y}!", x, y);
```

``` console --session LogInfoWithNamedTemplateParams
2020-10-15T02:03:37.4519291Z [Demo.Examples]  ℹ Hello world! The time is 10/14/2020 7:03:37 PM! 

```

The names of the tokens don't need to match the variable names, though it can be useful if they match because PocketLogger will capture those names along with the values, allowing you to provide more informative telemetry, for example when you're writing your telemetry to an API like Application Insights and plan to query it based on those names later.

These structured logging and message token behaviors apply to all of the `Log` methods, not just `Log.Info`.

### `Log.Event`

You'll typically log an event if the goal is to aggregate metrics of some sort. You can pass `ValueTuple`s to specify names and values for the metrics you'd like to capture.

``` csharp --source-file ./demo/Examples.cs --project ./demo/demo.csproj --region LogEvent --session LogEvent
Log.Event("LoudNoise", ("loudness", 9000), ("nearness", 1.5));
```

``` console --session LogEvent
2020-10-15T02:03:38.5686319Z [Demo.Examples] [LoudNoise]  📊  +[ (loudness, 9000), (nearness, 1.5) ] 

```

### `Log.Warning`

You can use `Log.Warning` to warn about things that need warning about. Maybe an exception was thrown that, while not a reason to panic, is definitely something you want to know happened. Maybe you want to swallow the exception, but you want to know you swallowed it. Best to log it.

``` csharp --source-file ./demo/Examples.cs --project ./demo/demo.csproj --region LogWarning --session LogWarning
Log.Warning("That's strange...", new DataMisalignedException());
```

``` console --session LogWarning
2020-10-15T02:03:39.7180819Z [Demo.Examples]  ⚠ That's strange... System.DataMisalignedException: A datatype misalignment was detected in a load or store instruction.

```

### `Log.Error`

When there's a real problem, you can use `Log.Error` to convey the seriousness of the situation.

``` csharp --source-file ./demo/Examples.cs --project ./demo/demo.csproj --region LogError --session LogError
Log.Error("Oh no! Run!", new BarrierPostPhaseException());
```

``` console --session LogError
2020-10-15T02:03:40.6984659Z [Demo.Examples]  ❌ Oh no! Run! System.Threading.BarrierPostPhaseException: The postPhaseAction failed with an exception.

```

## Operations

So that's all very nice, but there's nothing exciting about it relative to other logging APIs, except maybe the emojis. So let's talk now about operations. The notion of a logical operation in your code includes a few different concepts:

* Timing of an operation
* Success or failure of an operation
* State of variables at at various points during an operation
* Correlation between operations, including parent/child call structures, both in-process and and across processes or machines

PocketLogger provides a number of APIs for capturing this information. These APIs have some common features. Most importantly, all of them use object creation and disposal to let you define the boundaries of an operation.

Let's look at the basics of these methods before going into detail on each of them.

### `Log.OnExit`

`Log.OnExit` returns an `OperationLogger` which emits a single log event on disposal. We'll declare it using a discard (`_`) here because we're not doing anything with it other than letting it get disposed when the `using` block exits.

``` csharp --source-file ./demo/Examples.cs --project ./demo/demo.csproj --region LogOnExit --session LogOnExit
using (var _ = Log.OnExit())
{
}
```

``` console --session LogOnExit
2020-10-15T02:03:42.0209406Z [00-f0d0fcee4c844d409162f7c5c5778165-076d93a00e5f0144-00] [Demo.Examples] [LogOnExit]  ⏹ (3.0446ms)  

```

You can see from the output that it captures the class name and the time stamp (in this case, the time that the operation completed), along with a pleasing ⏹ symbol. It also captures the method name and the duration of the operation. The final difference is a long random-looking string in `[` square brackets `]`. This is a correlation id, generated by [`System.Diagnostics.Activity`](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.activity).

### `Log.OnEnterAndExit`

This method is very similar to `Log.OnExit` but, as the name says, it will log when the operation starts ▶ in addition to when it ends ⏹. This generates two log events rather than one.

``` csharp --source-file ./demo/Examples.cs --project ./demo/demo.csproj --region LogOnEnterAndExit --session LogOnEnterAndExit
using (var _ = Log.OnEnterAndExit())
{
}
```

``` console --session LogOnEnterAndExit
2020-10-15T02:03:43.0829988Z [00-3ea2139f17d2764190c1fa6d81d50142-88e56e06b9b4d141-00] [Demo.Examples] [LogOnEnterAndExit]  ▶  
2020-10-15T02:03:43.1028598Z [00-3ea2139f17d2764190c1fa6d81d50142-88e56e06b9b4d141-00] [Demo.Examples] [LogOnEnterAndExit]  ⏹ (21.6814ms)  

```

You can see from the output that the correlation ids from these two events match. They are part of the same logical activity.

### `Log.ConfirmOnExit`

Sometimes you want to know more than when an operation starts and ends. You might also want to know whether it was successful. A common use case is that you want to know, while looking at your telemetry, whether a method exited due to an exception. Scattering `try`/`catch` blocks around your code just to capture telemetry is a little too cluttered for some people. To avoid this, PocketLogger provides `Log.ConfirmOnExit` and `Log.OnEnterAndConfirmOnExit`. Rather than returning an `OperationLogger`, these methods return a `ConfirmationLogger`.

Here's a simple example:

``` csharp --source-file ./demo/Examples.cs --project ./demo/demo.csproj --region LogConfirmOnExit --session LogConfirmOnExit
using (var operation = Log.ConfirmOnExit())
{
}
```

``` console --session LogConfirmOnExit
2020-10-15T02:03:44.3649797Z [00-ab856a3c98c79e4d86ee86d6f5c58b55-8761b17d22b5994d-00] [Demo.Examples] [LogConfirmOnExit]  ⏹ -> ❌ (4.9807ms)  

```

Notice that the exit event indicates with a mildly alarming ⏹ -> ❌ that the operation has failed. The reason this happens is that the `ConfirmationLogger` assumes failure unless you confirm that the operation was successful. You can do this by calling `ConfirmationLogger.Succeed`:

``` csharp --source-file ./demo/Examples.cs --project ./demo/demo.csproj --region LogConfirmOnExitSucceed --session LogConfirmOnExitSucceed
using (var operation = Log.ConfirmOnExit())
{
    operation.Succeed();
}
```

``` console --session LogConfirmOnExitSucceed
2020-10-15T02:03:45.4853518Z [00-648fe10f014fea4fa2bfe860836580b2-3f85684644761241-00] [Demo.Examples] [LogConfirmOnExitSucceed]  ⏹ -> ✔ (4.5209ms)  

```

Ah, much better: ⏹ -> ✔

You can also proactively indicate that an operation has failed by calling `ConfirmationLogger.Fail`:

``` csharp --source-file ./demo/Examples.cs --project ./demo/demo.csproj --region LogConfirmOnExitFail --session LogConfirmOnExitFail
using (var operation = Log.ConfirmOnExit())
{
    operation.Fail();
}
```

``` console --session LogConfirmOnExitFail
2020-10-15T02:03:46.5520662Z [00-33439b22bc82594eafbb66f8e08b7917-4d72b26185b02642-00] [Demo.Examples] [LogConfirmOnExitFail]  ⏹ -> ❌ (4.3392ms)  

```

### `Log.OnEnterAndConfirmOnExit`

Just as with `Log.OnEnter` and `Log.OnEnterAndExit`, the confirmation methods provide two alternatives so that you can decide whether you want a log event to be posted at the start of the operation. Behold, `Log.OnEnterAndConfirmOnExit`:

``` csharp --source-file ./demo/Examples.cs --project ./demo/demo.csproj --region LogOnEnterAndConfirmOnExit --session LogOnEnterAndConfirmOnExit
using (var operation = Log.OnEnterAndConfirmOnExit())
{
    operation.Succeed();
}
```

``` console --session LogOnEnterAndConfirmOnExit
2020-10-15T02:03:47.5922984Z [00-e27998ddc5f84d498788d9454b1a1042-9930317b592d064d-00] [Demo.Examples] [LogOnEnterAndConfirmOnExit]  ▶  
2020-10-15T02:03:47.6258805Z [00-e27998ddc5f84d498788d9454b1a1042-9930317b592d064d-00] [Demo.Examples] [LogOnEnterAndConfirmOnExit]  ⏹ -> ✔ (36.2607ms)  

```

### Adding information to operation logs

Operations can be augumented with additional information at any point. All of the methods avaible on `Logger` (in other words, the `Log` APIs described earlier) are also available on the `OperationLogger` and `ConfirmationLogger` classes because they inherit from it.

In this next example, you can see a few different ways to post additional log entries on an operation.

``` csharp --source-file ./demo/Examples.cs --project ./demo/demo.csproj --region Checkpoints --session Checkpoints
using (var operation = Log.OnEnterAndConfirmOnExit())
{
    var apiResult = await CallSomeApiAsync();

    operation.Event("CalledSomeApi", ("ResultCode", apiResult));

    operation.Succeed("We did it!");
}
```

``` console --session Checkpoints
2020-10-15T02:03:48.6328897Z [00-ac3b89867d845b49ac4db310a3c4788d-39c10e85a3424947-00] [Demo.Examples] [Checkpoints]  ▶  
2020-10-15T02:03:48.7760502Z [00-ac3b89867d845b49ac4db310a3c4788d-39c10e85a3424947-00] [Demo.Examples] [CalledSomeApi]  📊 (146.2778ms)  +[ (ResultCode, 42) ] 
2020-10-15T02:03:48.7803699Z [00-ac3b89867d845b49ac4db310a3c4788d-39c10e85a3424947-00] [Demo.Examples] [Checkpoints]  ⏹ -> ✔ (150.5962ms) We did it! 

```

As you can see in the output, calling these methods during the operation creates outputs with the same activity id as the start and stop events. It also allows you to record intermediate timings.

## Tracking changing variable state with exit args

Sometimes it's useful to track changes to a variable over the course of an operation. For this, PocketLogger operations have the concept of `exitArgs`, which can be specified when you start an operation. The `exitArgs` consist an array of `ValueTuple`s, which will be evaluated when the operation has completed.

``` csharp --source-file ./demo/Examples.cs --project ./demo/demo.csproj --region ExitArgs --session ExitArgs
var myVariable = "initial value";

using (var operation = Log.OnEnterAndConfirmOnExit(
    exitArgs: () => new[] { (nameof(myVariable), (object)myVariable) }))
{
    operation.Info("", (nameof(myVariable), myVariable));

    myVariable = "new value";

    operation.Succeed("Yes!");
}
```

``` console --session ExitArgs
2020-10-15T02:03:49.9461594Z [00-0e7d16fe6a188c42b766cfcca736aab6-3df8f6a8ec679547-00] [Demo.Examples] [ExitArgs]  ▶  
2020-10-15T02:03:49.9779749Z [00-0e7d16fe6a188c42b766cfcca736aab6-3df8f6a8ec679547-00] [Demo.Examples] [ExitArgs]  ℹ (34.626ms)  +[ (myVariable, initial value) ] 
2020-10-15T02:03:49.9815636Z [00-0e7d16fe6a188c42b766cfcca736aab6-3df8f6a8ec679547-00] [Demo.Examples] [ExitArgs]  ⏹ -> ✔ (38.214ms) Yes! +[ (myVariable, new value) ] 

```

## Nested operations and correlation

Just because the examples here have shown operations that are contained within a single method, that doesn't mean you'll always use PocketLogger this way. Complex operations can span multiple method calls and you can create child operations, creating new but related activity ids. Here's an example:

``` csharp --source-file ./demo/Examples.cs --project ./demo/demo.csproj --region ChildOperations --session ChildOperations
public static async Task Method1()
{
    using var operation = Log.OnEnterAndConfirmOnExit();

    await Method2(operation);

    operation.Succeed();
}

private static async Task Method2(ConfirmationLogger operation)
{
    using var childOperation = operation.OnEnterAndConfirmOnExit();

    childOperation.Succeed();
}
```

``` console --session ChildOperations
2020-10-15T02:03:51.1593849Z [00-e1acf79020105941813eb346610c1708-31526b89e8b5fa41-00] [Demo.Examples] [Method1]  ▶  
2020-10-15T02:03:51.1815393Z [00-e1acf79020105941813eb346610c1708-62a2a5549807d44b-00] [Demo.Examples] [Method2]  ▶  
2020-10-15T02:03:51.1826597Z [00-e1acf79020105941813eb346610c1708-62a2a5549807d44b-00] [Demo.Examples] [Method2]  ⏹ -> ✔ (1.1682ms)  
2020-10-15T02:03:51.1841508Z [00-e1acf79020105941813eb346610c1708-31526b89e8b5fa41-00] [Demo.Examples] [Method1]  ⏹ -> ✔ (26.5825ms)  

```

