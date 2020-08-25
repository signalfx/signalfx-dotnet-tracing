// Modified by SignalFx
using System;
using System.Collections.Generic;

namespace Datadog.Trace.TestHelpers
{
    public interface IMockSpan
    {
            public Guid TraceId { get; }

            public ulong SpanId { get; }

            public string Name { get; set; }

            public string Resource { get; set; }

            public string Service { get; }

            public string Type { get; set; }

            public long Start { get; }

            public long Duration { get; set; }

            public ulong? ParentId { get; }

            public byte Error { get; set; }

            public Dictionary<string, string> Tags { get; set; }

            public Dictionary<string, double> Metrics { get; set; }
    }
}
