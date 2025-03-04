// <copyright file="TracesTransportStrategy.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;
using Datadog.Trace.Agent.StreamFactories;
using Datadog.Trace.Agent.Transports;
using Datadog.Trace.Configuration;
using Datadog.Trace.HttpOverStreams;
using Datadog.Trace.Logging;

namespace Datadog.Trace.Agent
{
    internal static class TracesTransportStrategy
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<Tracer>();

        public static IApiRequestFactory Get(ImmutableExporterSettings settings)
        {
            var strategy = settings.TracesTransport;

            switch (strategy)
            {
                case TracesTransportType.WindowsNamedPipe:
                    Log.Information<string, string, int>("Using {FactoryType} for trace transport, with pipe name {PipeName} and timeout {Timeout}ms.", nameof(NamedPipeClientStreamFactory), settings.TracesPipeName, settings.TracesPipeTimeoutMs);
                    // use http://localhost as base endpoint
                    return new HttpStreamRequestFactory(new NamedPipeClientStreamFactory(settings.TracesPipeName, settings.TracesPipeTimeoutMs), DatadogHttpClient.CreateTraceAgentClient(), new Uri("http://localhost"));
                case TracesTransportType.Default:
                default:
#if NETCOREAPP
                    Log.Information("Using {FactoryType} for trace transport.", nameof(HttpClientRequestFactory));
                    return new HttpClientRequestFactory(settings.AgentUri, AgentHttpHeaderNames.DefaultHeaders);
#else
                    Log.Information("Using {FactoryType} for trace transport.", nameof(ApiWebRequestFactory));
                    return new ApiWebRequestFactory(settings.AgentUri, AgentHttpHeaderNames.DefaultHeaders);
#endif
            }
        }
    }
}
