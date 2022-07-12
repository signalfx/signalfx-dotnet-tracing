// Modified by Splunk Inc.

using System.Globalization;
using System.Text;
using Datadog.Trace.Configuration;

namespace Datadog.Trace.AlwaysOnProfiler
{
    internal class PlainTextThreadSampleExporter : ThreadSampleExporter
    {
        internal PlainTextThreadSampleExporter(ImmutableTracerSettings tracerSettings, ILogSender logSender)
        : base(tracerSettings, logSender)
        {
        }

        protected override string CreateBody(ThreadSample threadSample)
        {
            // The stack follows the experimental GDI conventions described at
            // https://github.com/signalfx/gdi-specification/blob/29cbcbc969531d50ccfd0b6a4198bb8a89cedebb/specification/semantic_conventions.md#logrecord-message-fields

            var stackTraceBuilder = new StringBuilder();

            stackTraceBuilder.AppendFormat(
                CultureInfo.InvariantCulture,
                "\"{0}\" #{1} prio=0 os_prio=0 cpu=0 elapsed=0 tid=0x{2:x} nid=0x{3:x}\n",
                threadSample.ThreadName,
                threadSample.ThreadIndex,
                threadSample.ManagedId,
                threadSample.NativeId);

            // TODO Splunk: APMI-2565 here should go Thread state, equivalent of"    java.lang.Thread.State: TIMED_WAITING (sleeping)"
            stackTraceBuilder.Append("\n");

            foreach (var frame in threadSample.Frames)
            {
                stackTraceBuilder.Append("\tat ");
                stackTraceBuilder.Append(frame);
                stackTraceBuilder.Append('\n');
            }

            return stackTraceBuilder.ToString();
        }
    }
}
