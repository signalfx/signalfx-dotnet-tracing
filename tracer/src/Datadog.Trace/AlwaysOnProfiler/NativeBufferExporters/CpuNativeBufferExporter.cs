using System;
using Datadog.Trace.ClrProfiler;
using Datadog.Trace.Logging;

namespace Datadog.Trace.AlwaysOnProfiler.NativeBufferExporters;

internal class CpuNativeBufferExporter : INativeBufferExporter
{
    private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(CpuNativeBufferExporter));
    private readonly ThreadSampleExporter _exporter;

    public CpuNativeBufferExporter(ThreadSampleExporter exporter)
    {
        _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
    }

    /// <summary>
    /// Exports cpu samples captured on the native side using provided buffer.
    /// </summary>
    /// <param name="buffer">the buffer to read captured cpu samples into</param>
    public void Export(byte[] buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }

        try
        {
            // Call twice in quick succession to catch up any blips; the second will likely return 0 (no buffer)
            ReadAndExportThreadSampleBatch(buffer);
            ReadAndExportThreadSampleBatch(buffer);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing cpu samples batch.");
        }
    }

    private void ReadAndExportThreadSampleBatch(byte[] buffer)
    {
        var read = NativeMethods.SignalFxReadThreadSamples(buffer.Length, buffer);
        if (read <= 0)
        {
            // No data just return.
            return;
        }

        var threadSamples = SampleNativeFormatParser.ParseThreadSamples(buffer, read);
        _exporter.ExportThreadSamples(threadSamples);
    }
}
