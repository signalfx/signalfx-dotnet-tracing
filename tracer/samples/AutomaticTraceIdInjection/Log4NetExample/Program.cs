using System.IO;
using log4net;
using log4net.Config;
using OpenTracing.Util;

namespace Log4NetExample
{
    public class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));

        public static void Main(string[] args)
        {
            var logRepository = LogManager.GetRepository(typeof(Program).Assembly);
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

            try
            {
                // Obtain the automatically registered OpenTracing.Util.GlobalTracer instance
                var tracer = GlobalTracer.Instance;

                LogicalThreadContext.Properties["order-number"] = 1024;
                log.Info("Message before a trace.");

                using (var scope = tracer.BuildSpan("Log4NetExample - Main()").StartActive(finishSpanOnDispose: true))
                {
                    log.Info("Message during a trace.");
                }
            }
            finally
            {
                LogicalThreadContext.Properties.Remove("order-number");
            }

            log.Info("Message after a trace.");
        }
    }
}
