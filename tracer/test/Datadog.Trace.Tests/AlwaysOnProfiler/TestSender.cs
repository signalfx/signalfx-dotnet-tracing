using System.Collections.Generic;
using Datadog.Trace.AlwaysOnProfiler;
using Datadog.Tracer.OpenTelemetry.Proto.Logs.V1;

namespace Datadog.Trace.Tests.AlwaysOnProfiler;

internal class TestSender : ILogSender
{
    public List<LogRecord> LogRecordSnapshots { get; } = new();

    public List<LogsData> SentLogData { get; } = new();

    public void Send(LogsData logsData)
    {
        SentLogData.Add(logsData);
        var logRecords = logsData.ResourceLogs[0].InstrumentationLibraryLogs[0].Logs;
        LogRecordSnapshots.AddRange(logRecords);
    }
}
