using System;
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.Configuration;
using Datadog.Trace.Logging;
using Datadog.Tracer.OpenTelemetry.Proto.Common.V1;
using Datadog.Tracer.OpenTelemetry.Proto.Logs.V1;
using Datadog.Tracer.OpenTelemetry.Proto.Resource.V1;

namespace Datadog.Trace.AlwaysOnProfiler;

/// <summary>
/// Exports Cpu/Allocation samples, accumulating LogRecords created by provided native buffer processors.
/// </summary>
internal class SampleExporter
{
    private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(SampleExporter));

    private readonly ILogSender _logSender;
    private readonly IList<ILogRecordAppender> _logRecordAppenders;

    private readonly LogsData _logsData;

    public SampleExporter(ImmutableTracerSettings tracerSettings, ILogSender logSender, IList<ILogRecordAppender> logRecordAppenders)
    {
        _logSender = logSender ?? throw new ArgumentNullException(nameof(logSender));
        _logRecordAppenders = logRecordAppenders ?? throw new ArgumentNullException(nameof(logRecordAppenders));
        // The same _logsData instance is used on all export messages. With the exception of the list of
        // LogRecords, the Logs property, all other fields are prepopulated.
        _logsData = CreateLogsData(tracerSettings);
    }

    public void Export()
    {
        var logRecords = _logsData.ResourceLogs[0].InstrumentationLibraryLogs[0].Logs;
        try
        {
            foreach (var logRecordAppender in _logRecordAppenders)
            {
                // Accumulate results in logRecords:
                // allocation samples appender creates at most 1 log record,
                // cpu samples appender can create 2
                logRecordAppender.AppendTo(logRecords);
            }

            if (logRecords.Count > 0)
            {
                _logSender.Send(_logsData);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing samples.");
        }
        finally
        {
            // The exporter reuses the _logsData object, but the actual log records are not
            // needed after serialization, release the log records so they can be garbage collected.
            logRecords.Clear();
        }
    }

    private static LogsData CreateLogsData(ImmutableTracerSettings tracerSettings)
    {
        var resource = new Resource();
        var profilingAttributes = OtelResource
                                 .GetCommonAttributes(tracerSettings, CorrelationIdentifier.Service)
                                 .Select(kv =>
                                             new KeyValue
                                             {
                                                 Key = kv.Key,
                                                 Value = new AnyValue
                                                 {
                                                     StringValue = kv.Value
                                                 }
                                             });
        resource.Attributes.AddRange(profilingAttributes);

        return new LogsData
        {
            ResourceLogs =
            {
                new ResourceLogs
                {
                    InstrumentationLibraryLogs =
                    {
                        new InstrumentationLibraryLogs
                        {
                            InstrumentationLibrary = GdiProfilingConventions.OpenTelemetry.InstrumentationLibrary
                        }
                    },
                    Resource = resource
                }
            }
        };
    }
}
