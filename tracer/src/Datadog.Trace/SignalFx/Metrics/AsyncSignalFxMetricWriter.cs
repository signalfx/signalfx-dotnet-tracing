using System;
using Datadog.Trace.Vendors.StatsdClient.Worker;
using Datadog.Tracer.SignalFx.Metrics.Protobuf;

namespace Datadog.Trace.SignalFx.Metrics;

internal class AsyncSignalFxMetricWriter : ISignalFxMetricWriter
{
    private readonly AsynchronousWorker<DataPoint> _worker;

    public AsyncSignalFxMetricWriter(ISignalFxMetricExporter exporter, int maxItems)
    {
        if (exporter == null)
        {
            throw new ArgumentNullException(nameof(exporter));
        }

        if (maxItems < 1)
        {
            throw new ArgumentException("MaxItems has to be greater or equal to 1", nameof(maxItems));
        }

        _worker = new AsynchronousWorker<DataPoint>(
            new BufferingWorker(exporter, maxItems),
            new Waiter(),
            1,
            maxItems,
            null);
    }

    public bool TryWrite(DataPoint dataPoint)
    {
        return _worker.TryEnqueue(dataPoint);
    }

    public void Dispose()
    {
        _worker.Dispose();
    }
}
