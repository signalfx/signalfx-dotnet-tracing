using System;
using System.Diagnostics;
using Datadog.Trace.Vendors.StatsdClient.Worker;
using Datadog.Tracer.SignalFx.Metrics.Protobuf;

namespace Datadog.Trace.SignalFx.Metrics;

/// <summary>
/// Buffers metric data points before sending them using injected exporter.
/// </summary>
internal class BufferingWorker : IAsynchronousWorkerHandler<DataPoint>
{
    private readonly ISignalFxMetricExporter _exporter;
    private readonly DataPointUploadMessage _uploadMessage;

    private readonly int _maxBufferSize;
    private readonly TimeSpan _idlePeriod;
    private Stopwatch _stopwatch;

    public BufferingWorker(ISignalFxMetricExporter exporter, int maxItems)
        : this(exporter, maxItems, InitUploadMessage)
    {
    }

    internal BufferingWorker(ISignalFxMetricExporter exporter, int maxItems, Func<DataPointUploadMessage> uploadMessageFactory)
    {
        _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
        if (uploadMessageFactory == null)
        {
            throw new ArgumentNullException(nameof(uploadMessageFactory));
        }

        // The same _uploadMessage instance is used on all export messages.
        _uploadMessage = uploadMessageFactory();
        if (maxItems < 1)
        {
            throw new ArgumentException("MaxItems has to be greater or equal to 1", nameof(maxItems));
        }

        _maxBufferSize = maxItems;
        // idle period similar to trace exporter
        _idlePeriod = TimeSpan.FromSeconds(1);
    }

    private static DataPointUploadMessage InitUploadMessage()
    {
        return new DataPointUploadMessage();
    }

    public void OnNewValue(DataPoint v)
    {
        _uploadMessage.datapoints.Add(v);
        if (_uploadMessage.datapoints.Count >= _maxBufferSize)
        {
            FlushBuffer();
        }

        _stopwatch = null;
    }

    public bool OnIdle()
    {
        if (_stopwatch == null)
        {
            _stopwatch = Stopwatch.StartNew();
        }

        if (_stopwatch.ElapsedMilliseconds > _idlePeriod.TotalMilliseconds)
        {
            FlushBuffer();
        }

        return true;
    }

    public void Flush()
    {
        FlushBuffer();
    }

    private void FlushBuffer()
    {
        try
        {
            _exporter.Send(_uploadMessage);
        }
        finally
        {
            // Release the data points so they can be garbage collected.
            _uploadMessage.datapoints.Clear();
        }
    }
}
