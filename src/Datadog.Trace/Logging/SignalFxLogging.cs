// Modified by SignalFx
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using SignalFx.Tracing.Configuration;
using SignalFx.Tracing.Vendors.Serilog;
using SignalFx.Tracing.Vendors.Serilog.Core;
using SignalFx.Tracing.Vendors.Serilog.Events;
using SignalFx.Tracing.Vendors.Serilog.Formatting.Display;
using SignalFx.Tracing.Vendors.Serilog.Sinks.File;

namespace SignalFx.Tracing.Logging
{
    internal static class SignalFxLogging
    {
        private const string NixDefaultDirectory = "/var/log/signalfx/dotnet";

        private static readonly long? MaxLogFileSize = 10 * 1024 * 1024;
        private static readonly LoggingLevelSwitch LoggingLevelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);
        private static readonly ILogger SharedLogger = null;

        static SignalFxLogging()
        {
            // No-op for if we fail to construct a logger.
            SharedLogger =
                new LoggerConfiguration()
                   .WriteTo.Sink<NullSink>()
                   .CreateLogger();

            LoggerConfiguration loggerConfiguration = null;
            var currentProcess = Process.GetCurrentProcess();
            try
            {
                if (GlobalSettings.Source.DebugEnabled)
                {
                    LoggingLevelSwitch.MinimumLevel = LogEventLevel.Verbose;
                }

                loggerConfiguration =
                    new LoggerConfiguration()
                       .Enrich.FromLogContext()
                       .MinimumLevel.ControlledBy(LoggingLevelSwitch);

                if (GlobalSettings.Source.StdoutLogEnabled)
                {
                    var outputTemplate = Environment.GetEnvironmentVariable(ConfigurationKeys.StdoutLogTemplate) ??
                        "[{Level:u3}] {Message:lj} {NewLine}{Exception}{NewLine}";

                    loggerConfiguration.WriteTo.Sink(new ConsoleSink(outputTemplate), LogEventLevel.Verbose);
                }

                try
                {
                    var currentAppDomain = AppDomain.CurrentDomain;
                    loggerConfiguration.Enrich.WithProperty("MachineName", currentProcess.MachineName);
                    loggerConfiguration.Enrich.WithProperty("ProcessName", currentProcess.ProcessName);
                    loggerConfiguration.Enrich.WithProperty("PID", currentProcess.Id);
                    loggerConfiguration.Enrich.WithProperty("AppDomainName", currentAppDomain.FriendlyName);
                }
                catch
                {
                    // At all costs, make sure the logger works when possible.
                }

                if (GlobalSettings.Source.FileLogEnabled)
                {
                    var maxLogSizeVar = Environment.GetEnvironmentVariable(ConfigurationKeys.MaxLogFileSize);
                    if (long.TryParse(maxLogSizeVar, out var maxLogSize))
                    {
                        // No verbose or debug logs
                        MaxLogFileSize = maxLogSize;
                    }

                    var logDirectory = GetLogDirectory();

                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    if (logDirectory == null)
                    {
                        SharedLogger = loggerConfiguration.CreateLogger();
                        return;
                    }

                    // Ends in a dash because of the date postfix
                    var managedLogPath = Path.Combine(logDirectory, $"dotnet-tracer-{currentProcess.ProcessName}-.log");
                    Console.WriteLine("[Info] managedLogPath: \"" + managedLogPath + "\"");

                    loggerConfiguration
                           .WriteTo.File(
                                managedLogPath,
                                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}{Properties}{NewLine}",
                                rollingInterval: RollingInterval.Day,
                                rollOnFileSizeLimit: true,
                                fileSizeLimitBytes: MaxLogFileSize);
                }

                SharedLogger = loggerConfiguration.CreateLogger();
            }
            catch (Exception ex)
            {
                // Don't let this exception bubble up as this logger is for debugging and is non-critical
                Console.WriteLine($"[Error] {nameof(SignalFxLogging)}: Failed to setup file logging: {ex.Message}");
            }
            finally
            {
                // Log some information to correspond with the app domain
                var msg = $"{FrameworkDescription.Create()} {{ MachineName: \"{currentProcess.MachineName}\", ProcessName: \"{currentProcess.ProcessName}\", PID: {currentProcess.Id}, AppDomainName: \"{AppDomain.CurrentDomain.FriendlyName}\" }}";
                SharedLogger.Information(msg);
            }
        }

        public static ILogger GetLogger(Type classType)
        {
            // Tells us which types are loaded, when, and how often.
            SharedLogger.Debug($"Logger retrieved for: {classType.AssemblyQualifiedName}");
            return SharedLogger;
        }

        public static ILogger For<T>()
        {
            return GetLogger(typeof(T));
        }

        internal static void SetLogLevel(LogEventLevel logLevel)
        {
            LoggingLevelSwitch.MinimumLevel = logLevel;
        }

        internal static void UseDefaultLevel()
        {
            SetLogLevel(LogEventLevel.Information);
        }

        private static string GetLogDirectory()
        {
            var nativeLogFile = Environment.GetEnvironmentVariable(ConfigurationKeys.ProfilerLogPath);
            string logDirectory = null;

            if (!string.IsNullOrEmpty(nativeLogFile))
            {
                logDirectory = Path.GetDirectoryName(nativeLogFile);
            }

            // This entire block may throw a SecurityException if not granted the System.Security.Permissions.FileIOPermission
            // because of the following API calls
            //   - Directory.Exists
            //   - Environment.GetFolderPath
            //   - Path.GetTempPath
            if (logDirectory == null)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var windowsDefaultDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"SignalFx .NET Tracing", "logs");
                    if (Directory.Exists(windowsDefaultDirectory))
                    {
                        logDirectory = windowsDefaultDirectory;
                    }
                }
                else
                {
                    // either Linux or OS X
                    if (Directory.Exists(NixDefaultDirectory))
                    {
                        logDirectory = NixDefaultDirectory;
                    }
                    else
                    {
                        try
                        {
                            var di = Directory.CreateDirectory(NixDefaultDirectory);
                            logDirectory = NixDefaultDirectory;
                        }
                        catch
                        {
                            // Unable to create the directory meaning that the user
                            // will have to create it on their own.
                        }
                    }
                }
            }

            if (logDirectory == null)
            {
                // Last effort at writing logs
                logDirectory = Path.GetTempPath();
            }

            return logDirectory;
        }

        private class ConsoleSink : ILogEventSink
        {
            private readonly MessageTemplateTextFormatter _formatter;

            public ConsoleSink(string outputTemplate)
            {
                outputTemplate = outputTemplate ?? throw new ArgumentNullException(nameof(outputTemplate));

                _formatter = new MessageTemplateTextFormatter(outputTemplate, null);
            }

            public void Emit(LogEvent logEvent)
            {
                _formatter.Format(logEvent, Console.Out);
            }
        }
    }
}
