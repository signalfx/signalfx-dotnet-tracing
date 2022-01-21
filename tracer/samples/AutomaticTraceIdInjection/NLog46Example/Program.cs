using NLog;
using OpenTracing;
using OpenTracing.Util;

namespace NLog46Example
{
    public class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static void Main(string[] args)
        {
            // Obtain the automatically registered OpenTracing.Util.GlobalTracer instance
            var tracer = GlobalTracer.Instance;

            using (MappedDiagnosticsLogicalContext.SetScoped("order-number", 1024))
            {
                Logger.Info("Message before a trace.");

                using (IScope scope = tracer.BuildSpan("NLog46Example - Main()").StartActive(finishSpanOnDispose: true))
                {
                    Logger.Info("Message during a trace.");
                }

                Logger.Info("Message after a trace.");
            }
        }
    }
}
