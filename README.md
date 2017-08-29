PocketLogger
============

[![Build Status](https://ci.appveyor.com/api/projects/status/github/jonsequitur/PocketLogger?svg=true&branch=master)](https://ci.appveyor.com/project/jonsequitur/PocketLogger) [![NuGet Status](http://img.shields.io/nuget/v/PocketLogger.svg?style=flat)](https://www.nuget.org/packages/PocketLogger/) 

PocketLogger is a code instrumentation library for logging and telemetry composed of individual C# files that are linked directly into your project using Nuget. It provides support for the common structured logging API shapes seen in libraries such as [Microsoft.Extensions.Logging](https://github.com/aspnet/Logging) and [Serilog](https://github.com/serilog). Since PocketLogger doesn't add assembly dependencies to your project, it's well-suited for use by library authors, much like [LibLog](https://github.com/damianh/LibLog), and it can also be used by application authors. 

PocketLogger's goals are:

* *Stay out of dependency injection*. Since logging and telemetry are typically cross-cutting concerns, PocketLogger has the opinion that 

* *Provide first-class concepts for correlation, operation timing, and operation success or failure.* PocketLogger provides a few concepts for describing the behavior of code. 

* *Be helpful as a development-time diagnostic tool.* PocketLogger makes it easy to add logging code anywhere in your code and write the output to the console from a test.

* *Make it easy to test*.

* *Don't cause application errors*.





