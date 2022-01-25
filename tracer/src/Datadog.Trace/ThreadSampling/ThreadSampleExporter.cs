// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.Net;
using Datadog.Trace.Configuration;
using Datadog.Trace.Logging;
using Datadog.Trace.Propagation;
using Datadog.Tracer.OpenTelemetry.Proto.Common.V1;
using Datadog.Tracer.OpenTelemetry.Proto.Logs.V1;
using Datadog.Tracer.OpenTelemetry.Proto.Resource.V1;

namespace Datadog.Trace.ThreadSampling
{
    internal class ThreadSampleExporter
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(ThreadSampleExporter));

        private readonly List<KeyValue> _fixedLogRecordAttributes;
        private readonly Uri _logsEndpointUrl;
        private readonly LogsData _logsData;

        internal ThreadSampleExporter(ImmutableTracerSettings tracerSettings)
        {
            _fixedLogRecordAttributes = new List<KeyValue>
            {
                GdiProfilingConventions.LogRecord.Attributes.Source,
                GdiProfilingConventions.LogRecord.Attributes.Period((long)tracerSettings.ThreadSamplingPeriod.TotalMilliseconds)
            };

            _logsEndpointUrl = tracerSettings.ExporterSettings.LogsEndpointUrl;

            _logsData = GdiProfilingConventions.CreateLogsData();
        }

        public void ExportThreadSamples(List<ThreadSample> threadSamples)
        {
            if (threadSamples == null || threadSamples.Count < 1)
            {
                return;
            }

            // Populate the actual log records.
            List<LogRecord> logRecords = _logsData.ResourceLogs[0].InstrumentationLibraryLogs[0].Logs;
            logRecords.Clear();

            for (int i = 0; i < threadSamples.Count; i++)
            {
                var logRecord = new LogRecord
                {
                    Attributes =
                    {
                        _fixedLogRecordAttributes[0],
                        _fixedLogRecordAttributes[1],
                    },
                    Body = new AnyValue { StringValue = threadSamples[i].StackTrace },
                    TimeUnixNano = threadSamples[i].Timestamp,
                };

                logRecords.Add(logRecord);
            }

            SendLogsData();
        }

        internal void SendLogsData()
        {
            var httpWebRequest = WebRequest.CreateHttp(_logsEndpointUrl);
            httpWebRequest.ContentType = "application/x-protobuf";
            httpWebRequest.Method = "POST";
            httpWebRequest.Headers.Add(CommonHttpHeaderNames.TracingEnabled, "false");

            using (var stream = httpWebRequest.GetRequestStream())
            {
                Vendors.ProtoBuf.Serializer.Serialize(stream, _logsData);
                stream.Flush();
            }

            // Release the log records as soon as possible.
            _logsData.ResourceLogs[0].InstrumentationLibraryLogs[0].Logs.Clear();

            try
            {
                using var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                if (httpWebResponse.StatusCode >= HttpStatusCode.OK && httpWebResponse.StatusCode < HttpStatusCode.MultipleChoices)
                {
                    return;
                }

                Log.Warning("HTTP error sending thread samples to {0}: {1}", _logsEndpointUrl, httpWebResponse.StatusCode);
            }
            catch (Exception ex)
            {
                Log.Error("Exception sending thread samples to {0}: {1}", _logsEndpointUrl, ex.Message);
            }
        }

        /// <summary>
        /// Holds the GDI profiling semantic conventions.
        /// <see href="https://github.com/signalfx/gdi-specification/blob/b09e176ca3771c3ef19fc9d23e8722fc77a3b6e9/specification/semantic_conventions.md#profiling-resourcelogs-message"/>
        /// </summary>
        private static class GdiProfilingConventions
        {
            public const string OpenTelemetryProfiling = "otel.profiling";
            public const string Version = "0.1.0";

            public static LogsData CreateLogsData()
            {
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
                                    InstrumentationLibrary = OpenTelemetry.InstrumentationLibrary,
                                },
                            },
                            Resource = OpenTelemetry.Resource
                        }
                    }
                };
            }

            private static class OpenTelemetry
            {
                public static readonly InstrumentationLibrary InstrumentationLibrary = new InstrumentationLibrary
                {
                    Name = OpenTelemetryProfiling,
                    Version = Version
                };

                public static readonly Resource Resource = new Resource
                {
                    Attributes =
                    {
                        new KeyValue { Key = CorrelationIdentifier.EnvKey, Value = new AnyValue { StringValue = CorrelationIdentifier.Env } },
                        new KeyValue { Key = CorrelationIdentifier.ServiceKey, Value = new AnyValue { StringValue = CorrelationIdentifier.Service } },
                        new KeyValue { Key = "telemetry.sdk.name", Value = new AnyValue { StringValue = "signalfx-" + TracerConstants.Library } },
                        new KeyValue { Key = "telemetry.sdk.language", Value = new AnyValue { StringValue = TracerConstants.Language } },
                        new KeyValue { Key = "telemetry.sdk.version", Value = new AnyValue { StringValue = TracerConstants.AssemblyVersion } },
                        new KeyValue { Key = "splunk.distro.version", Value = new AnyValue { StringValue = TracerConstants.AssemblyVersion } }
                    }
                };
            }

            public static class LogRecord
            {
                public static class Attributes
                {
                    public static readonly KeyValue Source = new KeyValue
                    {
                        Key = "com.splunk.sourcetype",
                        Value = new AnyValue { StringValue = OpenTelemetryProfiling }
                    };

                    public static KeyValue Period(long periodMilliseconds)
                    {
                        return new KeyValue
                        {
                            Key = "source.event.period",
                            Value = new AnyValue { IntValue = periodMilliseconds },
                        };
                    }
                }
            }
        }
    }
}
