// Modified by Splunk Inc.


using System;
using System.IO;
using PluginApplication;
using Serilog;
using Serilog.Core;
using Serilog.Formatting.Json;
using LogEventLevel = Serilog.Events.LogEventLevel;

namespace LogsInjection.Serilog
{
    public class Program
    {
        public static int Main(string[] args)
        {
            // This test creates and unloads an appdomain
            // It seems that in some (unknown) conditions the tracer gets loader into the child appdomain
            // When that happens, there is a risk that the startup log thread gets aborted during appdomain unload,
            // adding error logs which in turn cause a failure in CI.
            // Disabling the startup log at the process level should prevent this.
            Environment.SetEnvironmentVariable("SIGNALFX_TRACE_STARTUP_LOGS", "0");

            LoggingMethods.DeleteExistingLogs();

            // Initialize Serilog
            var appDirectory = Directory.GetParent(typeof(Program).Assembly.Location).FullName;
            var textFilePath = Path.Combine(appDirectory, "log-textFile.log");
            var jsonFilePath = Path.Combine(appDirectory, "log-jsonFile.log");

            var log = new LoggerConfiguration()
                                        .Enrich.FromLogContext()
                                        .MinimumLevel.Is(LogEventLevel.Information)
                                        .WriteTo.File(
                                            textFilePath,
                                            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] {{ service_name: \"{service_name}\", service_version: \"{service_version}\", deployment_environment: \"{deployment_environment}\", trace_id: \"{trace_id}\", span_id: \"{span_id}\" }} {Message:lj} {NewLine}{Exception}")
#if SERILOG_2_0
                                        .WriteTo.File(
                                            new JsonFormatter(),
                                            jsonFilePath)
#endif
                                        .CreateLogger();

            return LoggingMethods.RunLoggingProcedure(LogWrapper(log));
        }

#if SERILOG_2_0
        public static Action<string> LogWrapper(Logger log)
        {
            return (string message) => log.Information(message);
        }
#else
        public static Action<string> LogWrapper(ILogger log)
        {
            return (string message) => log.Information(message);
        }
#endif
    }
}
