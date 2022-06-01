// <copyright file="TelemetryTransportStrategy.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using Datadog.Trace.Agent;
using Datadog.Trace.Agent.StreamFactories;
using Datadog.Trace.Agent.Transports;
using Datadog.Trace.Configuration;
using Datadog.Trace.HttpOverStreams;
using Datadog.Trace.Logging;
using Datadog.Trace.Util;

namespace Datadog.Trace.Telemetry.Transports;

internal static class TelemetryTransportStrategy
{
    private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<Tracer>();
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    public static IApiRequestFactory GetDirectIntakeFactory(Uri baseEndpoint, string apiKey)
    {
#if NETCOREAPP
        Log.Information("Using {FactoryType} for telemetry transport direct to intake.", nameof(HttpClientRequestFactory));
        return new HttpClientRequestFactory(baseEndpoint, TelemetryHttpHeaderNames.GetDefaultIntakeHeaders(apiKey), timeout: Timeout);
#else
        Log.Information("Using {FactoryType} for telemetry transport direct to intake.", nameof(ApiWebRequestFactory));
        return new ApiWebRequestFactory(baseEndpoint, TelemetryHttpHeaderNames.GetDefaultIntakeHeaders(apiKey), timeout: Timeout);
#endif
    }

    public static IApiRequestFactory GetAgentIntakeFactory(ImmutableExporterSettings settings)
    {
        // use the same transport for telemetry as we do for traces
        var strategy = settings.TracesTransport;

        switch (strategy)
        {
            case TracesTransportType.WindowsNamedPipe:
                Log.Information<string, string, int>("Using {FactoryType} for telemetry transport, with pipe name {PipeName} and timeout {Timeout}ms.", nameof(NamedPipeClientStreamFactory), settings.TracesPipeName, settings.TracesPipeTimeoutMs);
                return new HttpStreamRequestFactory(new NamedPipeClientStreamFactory(settings.TracesPipeName, settings.TracesPipeTimeoutMs), DatadogHttpClient.CreateTelemetryAgentClient(), GetBaseEndpoint());
            case TracesTransportType.Default:
            default:
                var agentUri = UriHelpers.Combine(settings.AgentUri, TelemetryConstants.AgentTelemetryEndpoint);
#if NETCOREAPP
                Log.Information("Using {FactoryType} for telemetry transport.", nameof(HttpClientRequestFactory));
                return new HttpClientRequestFactory(agentUri, TelemetryHttpHeaderNames.GetDefaultAgentHeaders(), timeout: Timeout);
#else
                Log.Information("Using {FactoryType} for telemetry transport.", nameof(ApiWebRequestFactory));
                return new ApiWebRequestFactory(agentUri, TelemetryHttpHeaderNames.GetDefaultAgentHeaders(), timeout: Timeout);
#endif
        }

        static Uri GetBaseEndpoint() => new Uri("http://localhost/" + TelemetryConstants.AgentTelemetryEndpoint);
    }
}
