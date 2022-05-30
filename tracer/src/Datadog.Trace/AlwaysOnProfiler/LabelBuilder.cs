// Modified by Splunk Inc.

using Datadog.Tracer.Pprof.Proto.Profile.V1;

namespace Datadog.Trace.AlwaysOnProfiler
{
    internal static class LabelBuilder
    {
        public static Label BuildTraceIdLabel(ProfileBuilder profileBuilder, long higher, long lower)
        {
            var index = profileBuilder.StringTable.Count;
            var traceId = $"{higher:x16}{lower:x16}";
            profileBuilder.StringTable.Add(traceId);

            return new Label
            {
                Key = ProfileBuilder.TraceIdIndex,
                Str = index
            };
        }

        public static Label BuildSpanIdLabel(ProfileBuilder profileBuilder, long spanId)
        {
            var index = profileBuilder.StringTable.Count;
            profileBuilder.StringTable.Add(spanId.ToString("x16"));
            return new Label
            {
                Key = ProfileBuilder.SpanIdIndex,
                Str = index
            };
        }

        public static Label BuildTimeLabel(ProfileBuilder profileBuilder, ulong time)
        {
            profileBuilder.StringTable.Add(time.ToString());
            return new Label
            {
                Key = ProfileBuilder.SpanIdIndex,
                Num = (long)time + long.MinValue,
                NumUnit = ProfileBuilder.TimeUnitIndex
            };
        }
    }
}
