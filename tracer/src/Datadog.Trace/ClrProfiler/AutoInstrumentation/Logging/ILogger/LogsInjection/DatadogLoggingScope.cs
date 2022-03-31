// <copyright file="DatadogLoggingScope.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Logging.ILogger
{
    internal class DatadogLoggingScope : IReadOnlyList<KeyValuePair<string, object>>
    {
        private readonly string _service;
        private readonly string _env;
        private readonly string _version;
        private readonly Tracer _tracer;
        private readonly string _cachedFormat;

        public DatadogLoggingScope()
            : this(Tracer.Instance)
        {
        }

        internal DatadogLoggingScope(Tracer tracer)
        {
            _tracer = tracer;
            _service = tracer.DefaultServiceName ?? string.Empty;
            _env = tracer.Settings.Environment ?? string.Empty;
            _version = tracer.Settings.ServiceVersion ?? string.Empty;
            _cachedFormat = string.Format(
                CultureInfo.InvariantCulture,
                "service.name:\"{0}\", deployment.environment:\"{1}\", service.version:\"{2}\"",
                _service,
                _env,
                _version);
        }

        public int Count => 5;

        public KeyValuePair<string, object> this[int index]
        {
            get
            {
                // For mismatch version support we need to keep requesting old keys.
                var distContext = _tracer.DistributedSpanContext;
                return index switch
                {
                    0 => new KeyValuePair<string, object>(CorrelationIdentifier.ServiceKey, _service),
                    1 => new KeyValuePair<string, object>(CorrelationIdentifier.EnvKey, _env),
                    2 => new KeyValuePair<string, object>(CorrelationIdentifier.VersionKey, _version),
                    3 => new KeyValuePair<string, object>(CorrelationIdentifier.TraceIdKey, (_tracer.ActiveScope?.Span.TraceId ?? TraceId.Zero).ToString()),
                    4 => new KeyValuePair<string, object>(CorrelationIdentifier.SpanIdKey, (_tracer.ActiveScope?.Span.SpanId ?? 0).ToString()),
                    _ => throw new ArgumentOutOfRangeException(nameof(index))
                };
            }
        }

        public override string ToString()
        {
            var spanContext = _tracer.DistributedSpanContext;
            if (spanContext is not null)
            {
                // For mismatch version support we need to keep requesting old keys.
                var hasTraceId = spanContext.TryGetValue(SpanContext.Keys.TraceId, out string traceId) ||
                                 spanContext.TryGetValue("trace-id", out traceId);
                var hasSpanId = spanContext.TryGetValue(SpanContext.Keys.ParentId, out string spanId) ||
                                spanContext.TryGetValue("parent-id", out spanId);
                if (hasTraceId && hasSpanId)
                {
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}, trace_id:\"{1}\", span_id:\"{2}\"",
                        _cachedFormat,
                        traceId,
                        spanId);
                }
            }

            return _cachedFormat;
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            yield return new KeyValuePair<string, object>(CorrelationIdentifier.ServiceKey, _service);
            yield return new KeyValuePair<string, object>(CorrelationIdentifier.EnvKey, _env);
            yield return new KeyValuePair<string, object>(CorrelationIdentifier.VersionKey, _version);

            var spanContext = _tracer.DistributedSpanContext;
            if (spanContext is not null)
            {
                // For mismatch version support we need to keep requesting old keys.
                var hasTraceId = spanContext.TryGetValue(SpanContext.Keys.TraceId, out string traceId) ||
                                 spanContext.TryGetValue("trace-id", out traceId);
                var hasSpanId = spanContext.TryGetValue(SpanContext.Keys.ParentId, out string spanId) ||
                                spanContext.TryGetValue("parent-id", out spanId);

                if (hasTraceId && hasSpanId)
                {
                    yield return new KeyValuePair<string, object>(CorrelationIdentifier.TraceIdKey, traceId);
                    yield return new KeyValuePair<string, object>(CorrelationIdentifier.SpanIdKey, spanId);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
