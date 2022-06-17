// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using Datadog.Trace.Configuration;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.Logging;
using Datadog.Trace.Propagation;
using Datadog.Tracer.OpenTelemetry.Proto.Common.V1;
using Datadog.Tracer.OpenTelemetry.Proto.Logs.V1;
using Datadog.Tracer.OpenTelemetry.Proto.Resource.V1;

namespace Datadog.Trace.AlwaysOnProfiler
{
    internal abstract class ThreadSampleExporter
    {
        protected static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(ThreadSampleExporter));

        protected ThreadSampleExporter(ImmutableTracerSettings tracerSettings)
        {
            FixedLogRecordAttributes = new ReadOnlyCollection<KeyValue>(new List<KeyValue>
            {
                GdiProfilingConventions.LogRecord.Attributes.Source,
                GdiProfilingConventions.LogRecord.Attributes.Period((long)tracerSettings.ThreadSamplingPeriod.TotalMilliseconds),
                GdiProfilingConventions.LogRecord.Attributes.Format(tracerSettings.ProfilerExportFormat),
                GdiProfilingConventions.LogRecord.Attributes.Type
            });

            LogsEndpointUrl = tracerSettings.ExporterSettings.LogsEndpointUrl;

            LogsData = GdiProfilingConventions.CreateLogsData(tracerSettings.GlobalTags);
        }

        protected ReadOnlyCollection<KeyValue> FixedLogRecordAttributes { get; }

        protected Uri LogsEndpointUrl { get; }

        protected LogsData LogsData { get; }

        public void ExportThreadSamples(List<ThreadSample> threadSamples)
        {
            if (threadSamples == null || threadSamples.Count < 1)
            {
                return;
            }

            // The same _logsData instance is used on all export messages. With the exception of the list of
            // LogRecords, the Logs property, all other fields are prepopulated. At this point the code just`
            // need to create a LogRecord for each thread sample and add it to the Logs list.
            var logRecords = LogsData.ResourceLogs[0].InstrumentationLibraryLogs[0].Logs;

            foreach (var threadSample in threadSamples)
            {
                var body = CreateBody(threadSample);
                var logRecord = CreateLogRecord(body, threadSample.Timestamp.Nanoseconds);

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

        protected abstract string CreateBody(ThreadSample threadSample);

        private void SendLogsData()
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

        private LogRecord CreateLogRecord(string body, ulong timeUnixNanoseconds)
        {
            return new LogRecord
            {
                Attributes =
                {
                    FixedLogRecordAttributes[0],
                    FixedLogRecordAttributes[1],
                    FixedLogRecordAttributes[2],
                    FixedLogRecordAttributes[3]
                },
                Body = new AnyValue { StringValue = body },
                TimeUnixNano = timeUnixNanoseconds,
            };
        }

        /// <summary>
        /// Holds the GDI profiling semantic conventions.
        /// <see href="https://github.com/signalfx/gdi-specification/blob/b09e176ca3771c3ef19fc9d23e8722fc77a3b6e9/specification/semantic_conventions.md#profiling-resourcelogs-message"/>
        /// </summary>
        private static class GdiProfilingConventions
        {
            private const string OpenTelemetryProfiling = "otel.profiling";
            private const string Version = "0.1.0";

            public static LogsData CreateLogsData(IEnumerable<KeyValuePair<string, string>> additionalResources)
            {
                var resource = OpenTelemetry.Resource;
                foreach (var kvp in additionalResources)
                {
                    resource.Attributes.Add(new KeyValue { Key = kvp.Key, Value = new AnyValue { StringValue = kvp.Value } });
                }

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
                public static readonly InstrumentationLibrary InstrumentationLibrary = new()
                {
                    Name = OpenTelemetryProfiling,
                    Version = Version
                };

                public static readonly Resource Resource = new()
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
                    public static readonly KeyValue Source = new()
                    {
                        Key = "com.splunk.sourcetype",
                        Value = new AnyValue { StringValue = OpenTelemetryProfiling }
                    };

                    public static readonly KeyValue Type = new()
                    {
                        Key = "profiling.data.type",
                        Value = new AnyValue { StringValue = "cpu" }
                    };

                    public static KeyValue Period(long periodMilliseconds)
                    {
                        return new KeyValue
                        {
                            Key = "source.event.period",
                            Value = new AnyValue { IntValue = periodMilliseconds }
                        };
                    }

                    public static KeyValue Format(ProfilerExportFormat profilerExportFormat)
                    {
                        return new KeyValue
                        {
                            Key = "profiling.data.format",
                            Value = new AnyValue
                            {
                                StringValue = profilerExportFormat switch
                                {
                                    ProfilerExportFormat.Pprof => "pprof-gzip-base64",
                                    ProfilerExportFormat.Text => "text",
                                    _ => throw new ArgumentOutOfRangeException(nameof(profilerExportFormat), profilerExportFormat, null)
                                }
                            }
                        };
                    }
                }
            }
        }
    }
}
