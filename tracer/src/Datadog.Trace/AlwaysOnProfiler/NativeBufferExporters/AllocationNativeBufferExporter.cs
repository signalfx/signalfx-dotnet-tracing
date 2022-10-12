using System;
using Datadog.Trace.ClrProfiler;
using Datadog.Trace.Logging;

namespace Datadog.Trace.AlwaysOnProfiler.NativeBufferExporters;

internal class AllocationNativeBufferExporter : INativeBufferExporter
{
    private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(AllocationNativeBufferExporter));
    private readonly ThreadSampleExporter _exporter;

    public AllocationNativeBufferExporter(ThreadSampleExporter exporter)
    {
        _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
    }

    /// <summary>
    /// Exports allocation samples captured on the native side using provided buffer.
    /// </summary>
    /// <param name="buffer">the buffer to read captured allocation samples into</param>
    public void Export(byte[] buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }

        try
        {
            // Managed-side calls to this method dictate buffer changeover, no catch up call needed
            var read = NativeMethods.SignalFxReadAllocationSamples(buffer.Length, buffer);
            if (read <= 0)
            {
                // No data just return.
                return;
            }

            var allocationSamples = SampleNativeFormatParser.ParseAllocationSamples(buffer, read);
            _exporter.ExportAllocationSamples(allocationSamples);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing allocation samples batch.");
        }
    }
}
