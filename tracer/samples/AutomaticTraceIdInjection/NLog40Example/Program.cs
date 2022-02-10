// Modified by Splunk Inc.

using NLog;
using OpenTracing.Util;

namespace NLog40Example
{
    public class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static void Main(string[] args)
        {
            // Obtain the automatically registered OpenTracing.Util.GlobalTracer instance
            var tracer = GlobalTracer.Instance;

            MappedDiagnosticsContext.Set("order-number", 1024.ToString());
            Logger.Info("Message before a trace.");

            using (var scope = tracer.BuildSpan("NLog45Example - Main()").StartActive(finishSpanOnDispose: true))
            {
                Logger.Info("Message during a trace.");
            }

            Logger.Info("Message after a trace.");
            MappedDiagnosticsContext.Remove("order-number");
        }
    }
}
