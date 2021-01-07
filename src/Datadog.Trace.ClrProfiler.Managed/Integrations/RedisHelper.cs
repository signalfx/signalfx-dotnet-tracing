// Modified by SignalFx
using System;
using SignalFx.Tracing;
using SignalFx.Tracing.Logging;

namespace Datadog.Trace.ClrProfiler.Integrations
{
    internal static class RedisHelper
    {
        private const string OperationName = "redis.command";

        private static readonly SignalFx.Tracing.Vendors.Serilog.ILogger Log = SignalFxLogging.GetLogger(typeof(RedisHelper));

        internal static Scope CreateScope(Tracer tracer, string integrationName, string componentName, string host, string port, string rawCommand)
        {
            if (!Tracer.Instance.Settings.IsIntegrationEnabled(integrationName))
            {
                // integration disabled, don't create a scope, skip this trace
                return null;
            }

            Scope scope = null;

            try
            {
                int separatorIndex = rawCommand.IndexOf(' ');
                string command;

                if (separatorIndex >= 0)
                {
                    command = rawCommand.Substring(0, separatorIndex);
                }
                else
                {
                    command = rawCommand;
                }

                scope = tracer.StartActive(command ?? OperationName, serviceName: tracer.DefaultServiceName);

                var span = scope.Span;
                span.SetTag(Tags.InstrumentationName, componentName);
                span.SetTag(Tags.DbType, SpanTypes.Redis);
                span.SetTag(Tags.SpanKind, SpanKinds.Client);
                if (Tracer.Instance.Settings.TagRedisCommands)
                {
                    span.SetTag(Tags.DbStatement, rawCommand);
                }

                span.SetTag(Tags.OutHost, host);
                span.SetTag(Tags.OutPort, port);

                // set analytics sample rate if enabled
                var analyticsSampleRate = tracer.Settings.GetIntegrationAnalyticsSampleRate(integrationName, enabledWithGlobalSetting: false);
                span.SetMetric(Tags.Analytics, analyticsSampleRate);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating or populating scope.");
            }

            return scope;
        }
    }
}
