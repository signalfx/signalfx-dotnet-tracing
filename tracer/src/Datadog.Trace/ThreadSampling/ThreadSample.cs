// Modified by Splunk Inc

namespace Datadog.Trace.ThreadSampling
{
    internal class ThreadSample
    {
        public ulong Timestamp { get; set; }

        public string StackTrace { get; set; }

        public long SpanId { get; set; }

        public long TraceIdHigh { get; set; }

        public long TraceIdLow { get; set; }
    }
}
