using Datadog.Tracer.OpenTelemetry.Proto.Common.V1;

namespace Datadog.Trace.AlwaysOnProfiler;

/// <summary>
/// Holds the GDI profiling semantic conventions.
/// <see href="https://github.com/signalfx/gdi-specification/blob/b09e176ca3771c3ef19fc9d23e8722fc77a3b6e9/specification/semantic_conventions.md#profiling-resourcelogs-message"/>
/// </summary>
internal static class GdiProfilingConventions
{
    private const string OpenTelemetryProfiling = "otel.profiling";
    private const string Version = "0.1.0";

    public static class OpenTelemetry
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
