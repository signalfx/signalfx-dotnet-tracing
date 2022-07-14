using System.Collections.Generic;
using Datadog.Trace.AlwaysOnProfiler;
using Datadog.Tracer.OpenTelemetry.Proto.Logs.V1;

namespace Datadog.Trace.Tests.AlwaysOnProfiler;

internal class TestSender : ILogSender
{
    public IList<LogRecord> SentLogs { get; } = new List<LogRecord>();

    public void Send(LogsData logsData)
    {
        var logRecord = logsData.ResourceLogs[0].InstrumentationLibraryLogs[0].Logs[0];
        SentLogs.Add(logRecord);
    }
}
