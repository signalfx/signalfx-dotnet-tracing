﻿using System;
using System.Collections.Generic;
using Datadog.Trace.ClrProfiler;
using Datadog.Trace.Logging;
using Datadog.Tracer.OpenTelemetry.Proto.Logs.V1;

namespace Datadog.Trace.AlwaysOnProfiler.LogRecordAppenders;

internal class CpuLogRecordAppender : ILogRecordAppender
{
    private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(CpuLogRecordAppender));
    private readonly ThreadSampleProcessor _threadSampleProcessor;
    private readonly byte[] _buffer;

    public CpuLogRecordAppender(ThreadSampleProcessor processor, byte[] buffer)
    {
        _threadSampleProcessor = processor ?? throw new ArgumentNullException(nameof(processor));
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
            // Call twice in quick succession to catch up any blips; the second will likely return 0 (no buffer)
            AddLogRecordFromThreadSamples(_buffer, results);
            AddLogRecordFromThreadSamples(_buffer, results);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing cpu samples batch.");
        }
    }

    private void AddLogRecordFromThreadSamples(byte[] buffer, List<LogRecord> logRecords)
    {
        var read = NativeMethods.SignalFxReadThreadSamples(buffer.Length, buffer);
        if (read <= 0)
        {
            // No data just return.
            return;
        }

        var threadSamples = SampleNativeFormatParser.ParseThreadSamples(buffer, read);
        var logRecord = _threadSampleProcessor.ProcessThreadSamples(threadSamples);

        if (logRecord != null)
        {
            logRecords.Add(logRecord);
        }
    }
}
