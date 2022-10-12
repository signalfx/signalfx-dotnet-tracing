using System;

namespace Datadog.Trace.AlwaysOnProfiler.NativeBufferExporters;

internal class SequentialNativeBufferExporter : INativeBufferExporter
{
    private readonly INativeBufferExporter _cpuNativeBufferExporter;
    private readonly INativeBufferExporter _allocationNativeBufferExporter;

    public SequentialNativeBufferExporter(INativeBufferExporter cpuNativeBufferExporter, INativeBufferExporter allocationNativeBufferExporter)
    {
        _cpuNativeBufferExporter = cpuNativeBufferExporter ?? throw new ArgumentNullException(nameof(cpuNativeBufferExporter));
        _allocationNativeBufferExporter = allocationNativeBufferExporter ?? throw new ArgumentNullException(nameof(allocationNativeBufferExporter));
    }

    /// <summary>
    /// Exports cpu and allocation samples sequentially, reusing the provided buffer.
    /// </summary>
    /// <param name="buffer">the buffer to read captured samples into</param>
    public void Export(byte[] buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }

        _cpuNativeBufferExporter.Export(buffer);
        _allocationNativeBufferExporter.Export(buffer);
    }
}
