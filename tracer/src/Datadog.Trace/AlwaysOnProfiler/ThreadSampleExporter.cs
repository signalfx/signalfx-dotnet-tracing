// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.Configuration;
using Datadog.Tracer.OpenTelemetry.Proto.Common.V1;
using Datadog.Tracer.OpenTelemetry.Proto.Logs.V1;
using Datadog.Tracer.OpenTelemetry.Proto.Resource.V1;

namespace Datadog.Trace.AlwaysOnProfiler
{
    internal abstract class ThreadSampleExporter
    {
        private readonly ILogSender _logSender;

        private readonly LogsData _logsData;

        private readonly KeyValue _format;

        protected ThreadSampleExporter(ImmutableTracerSettings tracerSettings, ILogSender logSender, string format)
        {
            // The same _logsData instance is used on all export messages. With the exception of the list of
            // LogRecords, the Logs property, all other fields are prepopulated.
            _logsData = GdiProfilingConventions.CreateLogsData(tracerSettings);
            _logSender = logSender ?? throw new ArgumentNullException(nameof(logSender));

            _format = GdiProfilingConventions.LogRecord.Attributes.Format(format);
            ProfilingDataTypeCpu = GdiProfilingConventions.LogRecord.Attributes.Type("cpu");
            ProfilingDataTypeAllocation = GdiProfilingConventions.LogRecord.Attributes.Type("allocation");
        }

        protected KeyValue ProfilingDataTypeCpu { get; }

        protected KeyValue ProfilingDataTypeAllocation { get; }

        public void ExportThreadSamples(List<ThreadSample> threadSamples)
        {
            if (threadSamples == null || threadSamples.Count < 1)
            {
                return;
            }

            ProcessThreadSamples(threadSamples);
            Export();
        }

        public void ExportAllocationSamples(List<AllocationSample> allocationSamples)
        {
            if (allocationSamples == null || allocationSamples.Count < 1)
            {
                return;
            }

            ProcessAllocationSamples(allocationSamples);
            Export();
        }

        /// <summary>
        /// Exports accumulated log records and clears the collection.
        /// </summary>
        private void Export()
        {
            try
            {
                _logSender.Send(_logsData);
            }
            finally
            {
                // The exporter reuses the _logsData object, but the actual log records are not
                // needed after serialization, release the log records so they can be garbage collected.
                _logsData.ResourceLogs[0].InstrumentationLibraryLogs[0].Logs.Clear();
            }
        }

        protected abstract void ProcessThreadSamples(List<ThreadSample> samples);

        protected abstract void ProcessAllocationSamples(List<AllocationSample> allocationSamples);

        protected LogRecord AddLogRecord(string body, KeyValue profilingDataType)
        {
            // The stack follows the experimental GDI conventions described at
            // https://github.com/signalfx/gdi-specification/blob/29cbcbc969531d50ccfd0b6a4198bb8a89cedebb/specification/semantic_conventions.md#logrecord-message-fields

            var logRecord = new LogRecord
            {
                Attributes =
                {
                    GdiProfilingConventions.LogRecord.Attributes.Source, profilingDataType, _format
                },
                Body = new AnyValue
                {
                    StringValue = body
                }
            };

            _logsData.ResourceLogs[0].InstrumentationLibraryLogs[0].Logs.Add(logRecord);
            return logRecord;
        }

        /// <summary>
        /// Holds the GDI profiling semantic conventions.
        /// <see href="https://github.com/signalfx/gdi-specification/blob/b09e176ca3771c3ef19fc9d23e8722fc77a3b6e9/specification/semantic_conventions.md#profiling-resourcelogs-message"/>
        /// </summary>
        internal static class GdiProfilingConventions
        {
            private const string OpenTelemetryProfiling = "otel.profiling";
            private const string Version = "0.1.0";

            public static LogsData CreateLogsData(ImmutableTracerSettings tracerSettings)
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
                                    InstrumentationLibrary = OpenTelemetry.InstrumentationLibrary
                                }
                            },
                            Resource = resource
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
            }

            public static class LogRecord
            {
                public static class Attributes
                {
                    public static readonly KeyValue Source = new()
                    {
                        Key = "com.splunk.sourcetype",
                        Value = new AnyValue
                        {
                            StringValue = OpenTelemetryProfiling
                        }
                    };

                    public static KeyValue Type(string sampleType)
                    {
                        return new KeyValue
                        {
                            Key = "profiling.data.type",
                            Value = new AnyValue
                            {
                                StringValue = sampleType
                            }
                        };
                    }

                    public static KeyValue Period(long periodMilliseconds)
                    {
                        return new KeyValue
                        {
                            Key = "source.event.period",
                            Value = new AnyValue
                            {
                                IntValue = periodMilliseconds
                            }
                        };
                    }

                    public static KeyValue Format(string format)
                    {
                        return new KeyValue
                        {
                            Key = "profiling.data.format",
                            Value = new AnyValue
                            {
                                StringValue = format
                            }
                        };
                    }
                }
            }
        }
    }
}
