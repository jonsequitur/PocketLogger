PocketLogger
============

[![Build Status](https://ci.appveyor.com/api/projects/status/github/jonsequitur/PocketLogger?svg=true&branch=master)](https://ci.appveyor.com/project/jonsequitur/PocketLogger)

PocketLogger is a code instrumentation library for logging and telemetry composed of a few C# files that are linked directly into your project using NuGet source-only packages. It provides support for the common structured logging API shapes seen in libraries such as [Microsoft.Extensions.Logging](https://github.com/aspnet/Logging) and [Serilog](https://github.com/serilog). Since PocketLogger doesn't add assembly dependencies to your project, it's well-suited for use by library authors, much like [LibLog](https://github.com/damianh/LibLog). It can also be used by application authors, of course.

To have a look at the API, start [here](./docs/readme.md).

PocketLogger's goals are:

* *Stay out of dependency injection wireup*. Since logging and telemetry are typically cross-cutting concerns, PocketLogger has the opinion that you should be able to quickly add logging statements to your code without having to change configurations or class definitions.

* *Be helpful as a development-time diagnostic tool.* PocketLogger makes it easy to add logging code anywhere and capture the output from tests and write to the console or a log file.

* *Provide first-class concepts for correlation, operation timing, and operation success or failure.* PocketLogger lets you write minimal code describing the outcomes of a block of code in terms of the code, not in terms of the telemetry destination. This makes it easy to integrate not just with various logging systems but also with richer telemetry APIs such as Application Insights. If you want to change outputs, you won't have to revisit your code.

* *Make it easy to test*. PocketLogger allows you to subscribe directly to all instrumentation events emitted by your code, without needing to reconfigure the code under test. This makes it simple to test your instrumentation.

* *Don't cause application errors*. If your instrumentation code throws, it shouldn't take down your application.

# PocketLogger is made up of several libraries

## PocketLogger [![NuGet Status](https://img.shields.io/nuget/v/PocketLogger.svg?style=flat)](https://www.nuget.org/packages/PocketLogger/)

The core PocketLogger package provides APIs for instrumenting your code so that it can publish rich information that can be used for logs, telemetry, and adding detail to test output.

The docs are [here](./docs/readme.md).

## PocketLogger.Subscribe [![NuGet Status](https://img.shields.io/nuget/v/PocketLogger.Subscribe.svg?style=flat)](https://www.nuget.org/packages/PocketLogger.Subscribe/) 

Typically, most of the projects in a solution don't need to listen to log events. For the ones that do, there's PocketLogger.Subscribe. This package provides methods for subscribing to log events emitted using PocketLogger, so that they can then be sent to the output of your choice.

The docs are [here](./docs/PocketLogger.Subscribe.md).

## PocketLogger.For.ApplicationInsights [![NuGet Status](https://img.shields.io/nuget/v/PocketLogger.For.ApplicationInsights.svg?style=flat)](https://www.nuget.org/packages/PocketLogger.For.ApplicationInsights/) 

PocketLogger.For.ApplicationInsights provides a simple adapter to publish PocketLogger events to Application Insights.

## PocketLogger.For.Xunit [![NuGet Status](https://img.shields.io/nuget/v/PocketLogger.For.Xunit.svg?style=flat)](https://www.nuget.org/packages/PocketLogger.For.Xunit/) 

PocketLogger.For.Xunit provides support for capturing PocketLogger events from XUnit tests.

## Pocket.Disposable [![NuGet Status](https://img.shields.io/nuget/v/Pocket.Disposable.svg?style=flat)](https://www.nuget.org/packages/Pocket.Disposable/) 

Pocket.Disposable is a tiny package that adds a single [Disposable.cs](./Pocket.Logger/Disposable.cs) file into your project for all your inline `IDisposable` needs.

