using System;
using System.Collections.Generic;
using Datadog.Trace.ClrProfiler;
using Datadog.Trace.Logging;
using Datadog.Tracer.OpenTelemetry.Proto.Logs.V1;

namespace Datadog.Trace.AlwaysOnProfiler.LogRecordAppenders;

internal class AllocationLogRecordAppender : ILogRecordAppender
{
    private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(AllocationLogRecordAppender));
    private readonly ThreadSampleProcessor _allocationSampleProcessor;
    private readonly byte[] _buffer;

    public AllocationLogRecordAppender(ThreadSampleProcessor processor, byte[] buffer)
    {
        _allocationSampleProcessor = processor ?? throw new ArgumentNullException(nameof(processor));
        _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
    }

    /// <inheritdoc />
    public void AppendTo(List<LogRecord> results)
    {
        if (results == null)
        {
            throw new ArgumentNullException(nameof(results));
        }

        try
        {
            // Managed-side calls to this method dictate buffer changeover, no catch up call needed
            var read = NativeMethods.SignalFxReadAllocationSamples(_buffer.Length, _buffer);
            if (read <= 0)
            {
                // No data just return.
                return;
            }

            var allocationSamples = SampleNativeFormatParser.ParseAllocationSamples(_buffer, read);
            var logRecord = _allocationSampleProcessor.ProcessAllocationSamples(allocationSamples);
            if (logRecord != null)
            {
                results.Add(logRecord);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing allocation samples batch.");
        }
    }
}
