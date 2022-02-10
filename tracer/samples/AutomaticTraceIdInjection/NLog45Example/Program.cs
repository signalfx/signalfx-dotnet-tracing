// Modified by Splunk Inc.

using NLog;
using OpenTracing.Util;

namespace NLog45Example
{
    public class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static void Main(string[] args)
        {
            // Obtain the automatically registered OpenTracing.Util.GlobalTracer instance
            var tracer = GlobalTracer.Instance;

            using (MappedDiagnosticsContext.SetScoped("order-number", 1024))
            {
                Logger.Info("Message before a trace.");

                using (var scope = tracer.BuildSpan("NLog45Example - Main()").StartActive(finishSpanOnDispose: true))
                {
                    Logger.Info("Message during a trace.");
                }

                Logger.Info("Message after a trace.");
            }
        }
    }
}
