// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.Net;
using Datadog.Trace.Configuration;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.Propagation;
using Datadog.Tracer.OpenTelemetry.Proto.Common.V1;
using Datadog.Tracer.OpenTelemetry.Proto.Logs.V1;

namespace Datadog.Trace.AlwaysOnProfiler
{
    internal class SimpleThreadSampleExporter : ThreadSampleExporter
    {
        internal SimpleThreadSampleExporter(ImmutableTracerSettings tracerSettings)
        : base(tracerSettings)
        {
        }

        public override void ExportThreadSamples(List<ThreadSample> threadSamples)
        {
            if (threadSamples == null || threadSamples.Count < 1)
            {
                return;
            }

            // The same _logsData instance is used on all export messages. With the exception of the list of
            // LogRecords, the Logs property, all other fields are prepopulated. At this point the code just`
            // need to create a LogRecord for each thread sample and add it to the Logs list.
            var logRecords = LogsData.ResourceLogs[0].InstrumentationLibraryLogs[0].Logs;

            for (var i = 0; i < threadSamples.Count; i++)
            {
                var threadSample = threadSamples[i];
                var logRecord = new LogRecord
                {
                    Attributes =
                    {
                        FixedLogRecordAttributes[0],
                        FixedLogRecordAttributes[1],
                    },
                    Body = new AnyValue { StringValue = threadSample.StackTrace },
                    TimeUnixNano = threadSample.Timestamp,
                };

                if (threadSample.SpanId != 0 || threadSample.TraceIdHigh != 0 || threadSample.TraceIdLow != 0)
                {
                    // TODO Splunk: Add tests and validate.
                    logRecord.SpanId = BitConverter.GetBytes(threadSample.SpanId);
                    logRecord.TraceId = BitConverter.GetBytes(threadSample.TraceIdHigh).Concat(BitConverter.GetBytes(threadSample.TraceIdLow));
                }

                logRecords.Add(logRecord);
            }

            SendLogsData();
        }

        internal void SendLogsData()
        {
            HttpWebRequest httpWebRequest;

            try
            {
                httpWebRequest = WebRequest.CreateHttp(LogsEndpointUrl);
                httpWebRequest.ContentType = "application/x-protobuf";
                httpWebRequest.Method = "POST";
                httpWebRequest.Headers.Add(CommonHttpHeaderNames.TracingEnabled, "false");

                using var stream = httpWebRequest.GetRequestStream();
                Vendors.ProtoBuf.Serializer.Serialize(stream, LogsData);
                stream.Flush();
            }
            catch (Exception ex)
            {
                Log.Error("Exception preparing request to send thread samples to {0}: {1}", LogsEndpointUrl, ex);
                return;
            }
            finally
            {
                // The exporter reuses the _logsData object, but the actual log records are not
                // needed after serialization, release the log records so they can be garbage collected.
                LogsData.ResourceLogs[0].InstrumentationLibraryLogs[0].Logs.Clear();
            }

            try
            {
                using var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                if (httpWebResponse.StatusCode >= HttpStatusCode.OK && httpWebResponse.StatusCode < HttpStatusCode.MultipleChoices)
                {
                    return;
                }

                Log.Warning("HTTP error sending thread samples to {0}: {1}", LogsEndpointUrl, httpWebResponse.StatusCode);
            }
            catch (Exception ex)
            {
                Log.Error("Exception sending thread samples to {0}: {1}", LogsEndpointUrl, ex.Message);
            }
        }
    }
}
