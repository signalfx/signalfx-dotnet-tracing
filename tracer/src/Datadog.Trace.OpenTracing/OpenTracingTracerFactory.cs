// <copyright file="OpenTracingTracerFactory.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;
using Datadog.Trace.Configuration;
using Datadog.Trace.Logging;
using OpenTracing.Util;

namespace Datadog.Trace.OpenTracing
{
    /// <summary>
    /// This class contains factory methods to instantiate an OpenTracing compatible tracer that sends data to DataDog
    /// </summary>
    public static class OpenTracingTracerFactory
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(OpenTracingTracerFactory));

        /// <summary>
        /// Create a new Datadog compatible ITracer implementation with the given parameters
        /// </summary>
        /// <param name="agentEndpoint">The agent endpoint where the traces will be sent (default is http://localhost:9411/api/v2/spans).</param>
        /// <param name="defaultServiceName">Default name of the service (default is the name of the executing assembly).</param>
        /// <param name="isDebugEnabled">Turns on all debug logging (this may have an impact on application performance).</param>
        /// <returns>A Datadog compatible ITracer implementation</returns>
        public static global::OpenTracing.ITracer CreateTracer(Uri agentEndpoint = null, string defaultServiceName = null, bool isDebugEnabled = false)
        {
            // Keep supporting this older public method by creating a TracerConfiguration
            // from default sources, overwriting the specified settings, and passing that to the constructor.
            var configuration = TracerSettings.FromDefaultSources();
            GlobalSettings.SetDebugEnabled(isDebugEnabled);

            if (agentEndpoint != null)
            {
                configuration.ExporterSettings.AgentUri = agentEndpoint;
            }

            if (defaultServiceName != null)
            {
                configuration.ServiceName = defaultServiceName;
            }

            Tracer.Configure(configuration);
            return new OpenTracingTracer(Tracer.Instance);
        }

        /// <summary>
        /// Registers a Tracer as the global tracer, if unregistered.
        /// </summary>
        /// <param name="tracer">Existing SignalFx Tracer instance.</param>
        /// <returns>True if the Tracer was successfully registered by this call, otherwise false.</returns>
        public static bool RegisterGlobalTracerIfAbsent(Tracer tracer)
        {
            var registeredSuccessfully = GlobalTracer.RegisterIfAbsent(WrapTracer(tracer));
            if (!registeredSuccessfully)
            {
                Log.Warning("There was an error registering the OpenTracing tracer.");
            }

            return registeredSuccessfully;
        }

        /// <summary>
        /// Create a new Datadog compatible ITracer implementation using an existing Datadog Tracer instance
        /// </summary>
        /// <param name="tracer">Existing Datadog Tracer instance</param>
        /// <returns>A Datadog compatible ITracer implementation</returns>
        public static global::OpenTracing.ITracer WrapTracer(Tracer tracer)
        {
            return new OpenTracingTracer(tracer);
        }
    }
}
