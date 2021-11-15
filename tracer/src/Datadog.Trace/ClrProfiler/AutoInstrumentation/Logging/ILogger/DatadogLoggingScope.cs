// <copyright file="DatadogLoggingScope.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

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
            var span = _tracer.ActiveScope?.Span;
            if (span is null)
            {
                return _cachedFormat;
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}, trace_id:\"{1}\", span_id:\"{2}\"",
                _cachedFormat,
                span.TraceId.ToString(),
                span.SpanId);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            var span = _tracer.ActiveScope?.Span;
            yield return new KeyValuePair<string, object>(CorrelationIdentifier.ServiceKey, _service);
            yield return new KeyValuePair<string, object>(CorrelationIdentifier.EnvKey, _env);
            yield return new KeyValuePair<string, object>(CorrelationIdentifier.VersionKey, _version);

            if (span is not null)
            {
                yield return new KeyValuePair<string, object>(CorrelationIdentifier.TraceIdKey, span.TraceId.ToString());
                yield return new KeyValuePair<string, object>(CorrelationIdentifier.SpanIdKey, span.SpanId.ToString());
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
