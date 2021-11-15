// <copyright file="CorrelationIdentifier.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;
using System.Diagnostics;
using Datadog.Trace.Abstractions;

namespace Datadog.Trace
{
    /// <summary>
    /// An API to access identifying values of the service and the active span
    /// </summary>
    public static class CorrelationIdentifier
    {
        internal static readonly string ServiceKey = "service.name";
        internal static readonly string VersionKey = "service.version";
        internal static readonly string EnvKey = "deployment.environment";
        internal static readonly string TraceIdKey = "trace_id";
        internal static readonly string SpanIdKey = "span_id";

        // Serilog property names require valid C# identifiers
        internal static readonly string SerilogServiceKey = "service_name";
        internal static readonly string SerilogVersionKey = "service_version";
        internal static readonly string SerilogEnvKey = "deployment_environment";

        /// <summary>
        /// Gets the name of the service
        /// </summary>
        public static string Service
        {
            get
            {
                return Tracer.Instance.DefaultServiceName ?? string.Empty;
            }
        }

        /// <summary>
        /// Gets the version of the service
        /// </summary>
        public static string Version
        {
            get
            {
                return Tracer.Instance.Settings.ServiceVersion ?? string.Empty;
            }
        }

        /// <summary>
        /// Gets the environment name of the service
        /// </summary>
        public static string Env
        {
            get
            {
                return Tracer.Instance.Settings.Environment ?? string.Empty;
            }
        }

        /// <summary>
        /// Gets the id of the active trace.
        /// </summary>
        /// <returns>The id of the active trace. If there is no active trace, returns zero.</returns>
        public static TraceId TraceId
        {
            get
            {
                return Tracer.Instance.ActiveScope?.Span == null
                           ? TraceId.Zero
                           : Tracer.Instance.ActiveScope.Span.TraceId;
            }
        }

        /// <summary>
        /// Gets the id of the active span.
        /// </summary>
        /// <returns>The id of the active span. If there is no active span, returns zero.</returns>
        public static ulong SpanId
        {
            get
            {
                return Tracer.Instance.ActiveScope?.Span?.SpanId ?? 0;
            }
        }
    }
}
