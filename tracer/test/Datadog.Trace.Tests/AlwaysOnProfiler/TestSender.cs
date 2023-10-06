using Datadog.Trace.AlwaysOnProfiler;

namespace Datadog.Trace.Tests.AlwaysOnProfiler;

internal class TestSender : IOtlpSender
{
    /*
    public List<LogRecord> LogRecordSnapshots { get; } = new();

    public List<LogsData> SentLogData { get; } = new();

    public void Send(LogsData logsData)
    {
        SentLogData.Add(logsData);
        var logRecords = logsData.ResourceLogs[0].InstrumentationLibraryLogs[0].Logs;
        LogRecordSnapshots.AddRange(logRecords);
    }
    */

    public void Send(object data)
    {
        throw new System.NotImplementedException();
    }
}
