// <copyright file="ISpanContext.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System.Collections.Generic;

namespace Datadog.Trace
{
    /// <summary>
    /// Span context interface.
    /// </summary>
    public interface ISpanContext : IReadOnlyDictionary<string, string>
    {
        /// <summary>
        /// Gets the trace identifier.
        /// </summary>
        TraceId TraceId { get; }

        /// <summary>
        /// Gets the span identifier.
        /// </summary>
        ulong SpanId { get; }

        /// <summary>
        /// Gets the service name to propagate to child spans.
        /// </summary>
        string ServiceName { get; }

        /// <summary>
        /// Gets the parent context.
        /// </summary>
        public ISpanContext Parent { get; }
    }
}
