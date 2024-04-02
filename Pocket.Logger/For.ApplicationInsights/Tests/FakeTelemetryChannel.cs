using System;
using Microsoft.ApplicationInsights.Channel;

namespace Pocket.For.ApplicationInsights.Tests;

public class FakeTelemetryChannel : ITelemetryChannel
{
    private readonly Action<ITelemetry> onSend;

    public FakeTelemetryChannel(Action<ITelemetry> onSend) => this.onSend = onSend;

    public void Send(ITelemetry item) => onSend(item);

    public void Flush()
    {
    }

    public void Dispose()
    {
    }

    public bool? DeveloperMode { get; set; }

    public string EndpointAddress { get; set; }
}