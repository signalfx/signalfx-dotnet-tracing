// <copyright file="ReadOnlySpanContext.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

namespace Datadog.Trace;

internal class ReadOnlySpanContext : ISpanContext
{
    public ReadOnlySpanContext(TraceId traceId, ulong spanId, string serviceName)
    {
        TraceId = traceId;
        SpanId = spanId;
        ServiceName = serviceName;
    }

    public TraceId TraceId { get; }

    public ulong SpanId { get; }

    public string ServiceName { get; }
}
