using System;
using Datadog.Tracer.SignalFx.Metrics.Protobuf;

namespace Datadog.Trace.SignalFx.Metrics;

internal interface ISignalFxMetricWriter : IDisposable
{
    bool TryWrite(DataPoint dataPoint);
}
