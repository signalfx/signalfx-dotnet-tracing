// Modified by Splunk Inc

using System.Collections.Generic;

namespace Datadog.Trace.AlwaysOnProfiler
{
    internal class ThreadSample
    {
        public ulong Timestamp { get; set; }

        public long SpanId { get; set; }

        public long TraceIdHigh { get; set; }

        public long TraceIdLow { get; set; }

        public int ManagedId { get; set; }

        public int NativeId { get; set; }

        public string ThreadName { get; set; }

        public string StackTrace { get; set; }

        public IList<string> Frames { get; } = new List<string>();
    }
}
