// <copyright file="DatadogContextPropagator.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

#nullable enable

using System.Globalization;
using Datadog.Trace.Propagation;
using Datadog.Trace.Tagging;

namespace Datadog.Trace.Propagators
{
    internal class DatadogContextPropagator : IContextInjector, IContextExtractor
    {
        public void Inject<TCarrier, TCarrierSetter>(SpanContext context, TCarrier carrier, TCarrierSetter carrierSetter)
            where TCarrierSetter : struct, ICarrierSetter<TCarrier>
        {
            var invariantCulture = CultureInfo.InvariantCulture;

            carrierSetter.Set(carrier, "trace-id", context.TraceId.ToString());
            carrierSetter.Set(carrier, "parent-id", context.SpanId.ToString(invariantCulture));

            if (context.Origin != null)
            {
                carrierSetter.Set(carrier, "origin", context.Origin);
            }

            var samplingPriority = context.TraceContext?.SamplingPriority ?? context.SamplingPriority;
            if (samplingPriority != null)
            {
#pragma warning disable SA1118 // Parameter should not span multiple lines
                carrierSetter.Set(
                    carrier,
                    "sampling-priority",
                    samplingPriority.Value switch
                    {
                        -1 => "-1",
                        0 => "0",
                        1 => "1",
                        2 => "2",
                        _ => samplingPriority.Value.ToString(invariantCulture)
                    });
#pragma warning restore SA1118 // Parameter should not span multiple lines
            }

            var propagationHeaderMaxLength = context.TraceContext?.Tracer.Settings.TagPropagationHeaderMaxLength ?? TagPropagation.OutgoingPropagationHeaderMaxLength;
            var propagatedTraceTags = context.TraceContext?.Tags.ToPropagationHeader(propagationHeaderMaxLength) ?? context.PropagatedTags;

            if (!string.IsNullOrEmpty(propagatedTraceTags))
            {
                carrierSetter.Set(carrier, DDHttpHeaderNames.PropagatedTags, propagatedTraceTags);
            }
        }

        public bool TryExtract<TCarrier, TCarrierGetter>(TCarrier carrier, TCarrierGetter carrierGetter, out SpanContext? spanContext)
            where TCarrierGetter : struct, ICarrierGetter<TCarrier>
        {
            spanContext = null;

            var traceIdAsUint64 = ParseUtility.ParseUInt64(carrier, carrierGetter, "trace-id");
            if (traceIdAsUint64 == null)
            {
                // a valid traceId is required to use distributed tracing
                return false;
            }

            var traceId = TraceId.CreateFromUlong(traceIdAsUint64.Value);
            if (traceId == TraceId.Zero)
            {
                // a valid traceId is required to use distributed tracing
                return false;
            }

            var parentId = ParseUtility.ParseUInt64(carrier, carrierGetter, "parent-id") ?? 0;
            var samplingPriority = ParseUtility.ParseInt32(carrier, carrierGetter, "sampling-priority");
            var origin = ParseUtility.ParseString(carrier, carrierGetter, "origin");
            var propagatedTraceTags = ParseUtility.ParseString(carrier, carrierGetter, DDHttpHeaderNames.PropagatedTags);

            spanContext = new SpanContext(traceId, parentId, samplingPriority, serviceName: null, origin)
                          {
                              PropagatedTags = propagatedTraceTags
                          };
            return true;
        }
    }
}
