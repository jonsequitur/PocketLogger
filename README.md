PocketLogger
============

[![Build Status](https://ci.appveyor.com/api/projects/status/github/jonsequitur/PocketLogger?svg=true&branch=master)](https://ci.appveyor.com/project/jonsequitur/PocketLogger) [![NuGet Status](http://img.shields.io/nuget/v/PocketLogger.svg?style=flat)](https://www.nuget.org/packages/PocketLogger/) 

PocketLogger is a code instrumentation library for logging and telemetry composed of a few C# files that are linked directly into your project using Nuget. It provides support for the common structured logging API shapes seen in libraries such as [Microsoft.Extensions.Logging](https://github.com/aspnet/Logging) and [Serilog](https://github.com/serilog). Since PocketLogger doesn't add assembly dependencies to your project, it's well-suited for use by library authors, much like [LibLog](https://github.com/damianh/LibLog). It can also be used by application authors. 

PocketLogger's goals are:

* *Stay out of dependency injection wireup*. Since logging and telemetry are typically cross-cutting concerns, PocketLogger has the opinion that you should be able to quickly add logging statements to your code without having to change configurations somewhere else. This helps PocketLogger to...

* *Be helpful as a development-time diagnostic tool.* PocketLogger makes it easy to add logging code anywhere and capture the output in a test.

* *Provide first-class concepts for correlation, operation timing, and operation success or failure.* PocketLogger provides a few concepts for describing the outcomes of a block of code. These semantics make it easy to integrate not just with logging systems but with richer telemetry APIs such as Application Insights.

* *Make it easy to test*. PocketLogger allows you to subscribe directly to all instrumentation events emitted by your code, without needing to reconfigure the code under test. This makes it simple to test your instrumentation.

* *Don't cause application errors*. Is your instrumentation code throws, it shouldn't take down your application.







