// <copyright file="RedisHelper.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;
using Datadog.Trace.Configuration;
using Datadog.Trace.Logging;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Redis
{
    internal static class RedisHelper
    {
        private const string OperationName = "redis.command";
        private const string ServiceName = "redis";

        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(RedisHelper));

        internal static Scope CreateScope(Tracer tracer, IntegrationId integrationId, string integrationName, string host, string port, string rawCommand)
        {
            if (!Tracer.Instance.Settings.IsIntegrationEnabled(integrationId))
            {
                // integration disabled, don't create a scope, skip this trace
                return null;
            }

            var parent = tracer.ActiveScope?.Span;
            if (parent != null &&
                parent.Type == SpanTypes.Redis &&
                parent.GetTag(Tags.InstrumentationName) != null)
            {
                return null;
            }

            string serviceName = tracer.Settings.GetServiceName(tracer, ServiceName);
            Scope scope = null;

            try
            {
                var tags = new RedisTags();
                tags.InstrumentationName = integrationName;

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

                scope = tracer.StartActiveInternal(command ?? OperationName, serviceName: tracer.DefaultServiceName, tags: tags);

                var span = scope.Span;
                span.Type = SpanTypes.Redis;
                span.ResourceName = command;
                span.LogicScope = OperationName;

                if (tracer.Settings.TagRedisCommands)
                {
                    tags.RawCommand = rawCommand;
                }

                tags.Host = host;
                tags.Port = port;

                tags.SetAnalyticsSampleRate(integrationId, tracer.Settings, enabledWithGlobalSetting: false);
                tracer.TracerManager.Telemetry.IntegrationGeneratedSpan(integrationId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating or populating scope.");
            }

            return scope;
        }
    }
}
