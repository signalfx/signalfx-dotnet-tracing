using System.Collections.Generic;

namespace Datadog.Trace.AlwaysOnProfiler
{
    internal class ThreadSample
    {
        public Time Timestamp { get; set; }

        public long SpanId { get; set; }

        public long TraceIdHigh { get; set; }

        public long TraceIdLow { get; set; }

        public int ManagedId { get; set; }

        public string ThreadName { get; set; }

        public IList<string> Frames { get; } = new List<string>();

        internal class Time
        {
            public Time(long milliseconds)
            {
                Nanoseconds = (ulong)milliseconds * 1_000_000u;
            }

            public ulong Nanoseconds { get; }
        }
    }
}
