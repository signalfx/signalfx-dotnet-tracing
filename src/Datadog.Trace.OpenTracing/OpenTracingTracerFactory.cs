// Modified by SignalFx
using System;
using System.Net.Http;
using OpenTracing;
using OpenTracing.Util;

namespace SignalFx.Tracing.OpenTracing
{
    /// <summary>
    /// This class contains factory methods to instantiate an OpenTracing compatible tracer that sends data to a backend.
    /// </summary>
    public static class OpenTracingTracerFactory
    {
        private static object _lock = new object();

        /// <summary>
        /// Create a new SignalFx compatible ITracer implementation with the given parameters
        /// </summary>
        /// <param name="agentEndpoint">The agent endpoint where the traces will be sent (default is http://localhost:9080).</param>
        /// <param name="defaultServiceName">Default name of the service (default is the name of the executing assembly).</param>
        /// <param name="isDebugEnabled">Turns on all debug logging (this may have an impact on application performance).</param>
        /// <returns>A SignalFx compatible ITracer implementation</returns>
        public static ITracer CreateTracer(Uri agentEndpoint = null, string defaultServiceName = null, bool isDebugEnabled = false)
        {
            return CreateTracer(agentEndpoint, defaultServiceName, null, isDebugEnabled);
        }

        /// <summary>
        /// Create a new SignalFx compatible ITracer implementation using an existing Tracer instance
        /// </summary>
        /// <param name="tracer">Existing tracer instance</param>
        /// <returns>A compatible ITracer implementation</returns>
        public static ITracer WrapTracer(Tracer tracer)
        {
            return new OpenTracingTracer(tracer);
        }

        /// <summary>
        /// Registers a Tracer as the global tracer, if unregistered.
        /// </summary>
        /// <param name="tracer">Existing tracer instance.</param>
        /// <returns>True if the Tracer was successfully registered by this call, otherwise false.</returns>
        public static bool RegisterGlobalTracer(Tracer tracer)
        {
           if (!GlobalTracer.IsRegistered())
            {
                lock (_lock)
                {
                    if (!GlobalTracer.IsRegistered())
                    {
                        GlobalTracer.Register(WrapTracer(tracer));
                        return true;
                    }
                }
            }

            return false;
        }

        internal static OpenTracingTracer CreateTracer(Uri agentEndpoint, string defaultServiceName, DelegatingHandler delegatingHandler, bool isDebugEnabled)
        {
            var tracer = Tracer.Create(agentEndpoint, defaultServiceName, isDebugEnabled);
            return new OpenTracingTracer(tracer);
        }
    }
}
