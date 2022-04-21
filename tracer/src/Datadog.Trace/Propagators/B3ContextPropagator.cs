// <copyright file="B3ContextPropagator.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;

namespace Datadog.Trace.Propagators
{
    internal class B3ContextPropagator : IContextInjector, IContextExtractor
    {
        /// <summary>
        /// B3 TraceId header
        /// </summary>
        public const string TraceId = "x-b3-traceid";

        /// <summary>
        /// B3 SpanId header
        /// </summary>
        public const string SpanId = "x-b3-spanid";

        /// <summary>
        /// B3 SpanId header
        /// </summary>
        public const string Sampled = "x-b3-sampled";

        public void Inject<TCarrier, TCarrierSetter>(SpanContext context, TCarrier carrier, TCarrierSetter carrierSetter)
            where TCarrierSetter : struct, ICarrierSetter<TCarrier>
        {
            var traceId = IsValidTraceId(context.RawTraceId, out _) ? context.RawTraceId : context.TraceId.ToString();
            var spanId = IsValidSpanId(context.RawSpanId) ? context.RawSpanId : context.SpanId.ToString("x16");
            var sampled = context.SamplingPriority > 0 ? "1" : "0";

            carrierSetter.Set(carrier, TraceId, traceId);
            carrierSetter.Set(carrier, SpanId, spanId);
            carrierSetter.Set(carrier, Sampled, sampled);
        }

        public bool TryExtract<TCarrier, TCarrierGetter>(TCarrier carrier, TCarrierGetter carrierGetter, out SpanContext? spanContext)
            where TCarrierGetter : struct, ICarrierGetter<TCarrier>
        {
            spanContext = null;

            try
            {
                var rawTraceId = ParseUtility.ParseString(carrier, carrierGetter, TraceId)?.Trim();
                var rawSpanId = ParseUtility.ParseString(carrier, carrierGetter, SpanId)?.Trim();
                var samplingPriority = ParseUtility.ParseInt32(carrier, carrierGetter, Sampled);

                if (IsValidTraceId(rawTraceId, out var traceId) && IsValidSpanId(rawSpanId))
                {
                    var parentId = ParseUtility.ParseFromHexOrDefault(rawSpanId);

                    spanContext = new SpanContext(traceId, parentId, samplingPriority, serviceName: null, null, rawTraceId, rawSpanId);
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool IsValidTraceId([NotNullWhen(true)] string? traceId, out TraceId parsedTraceId)
        {
            if (traceId == null)
            {
                parsedTraceId = Trace.TraceId.Zero;
                return false;
            }

            parsedTraceId = Tracer.Instance.TracerManager.TraceIdConvention.CreateFromString(traceId);
            return parsedTraceId != Trace.TraceId.Zero;
        }

        private bool IsValidSpanId([NotNullWhen(true)] string? spanId)
        {
            if (string.IsNullOrEmpty(spanId))
            {
                return false;
            }

            if (spanId!.Length != 16)
            {
                return false;
            }

            return true;
        }
    }
}
