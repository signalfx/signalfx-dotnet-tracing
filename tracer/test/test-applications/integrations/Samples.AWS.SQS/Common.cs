// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using Amazon.SQS.Model;
using Datadog.Trace;
using Datadog.Trace.Propagation;
using Newtonsoft.Json;

namespace Samples.AWS.SQS
{
    public class Common
    {
        public static void AssertDistributedTracingHeaders(List<Message> messages)
        {
            foreach (var message in messages)
            {
                Dictionary<string, string> dictSpanContext = new();
                var jsonSpanContext = message.MessageAttributes["_datadog"]?.StringValue;
                if (jsonSpanContext is not null)
                {
                    dictSpanContext = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonSpanContext);
                }

                if (dictSpanContext.TryGetValue(DDHttpHeaderNames.ParentId, out var parentId))
                {
                    if (parentId is null ||
                    !VerifyTraceId(Tracer.Instance.ActiveScope.Span.TraceId, dictSpanContext[DDHttpHeaderNames.TraceId]))
                    {
                        throw new Exception($"The span context was not injected into the message properly. parent-id: {dictSpanContext[DDHttpHeaderNames.ParentId]}, trace-id: {dictSpanContext[DDHttpHeaderNames.TraceId]}, active trace-id: {Tracer.Instance.ActiveScope.Span.TraceId}");
                    }
                }
                else if (dictSpanContext.TryGetValue(B3HttpHeaderNames.B3SpanId, out var spanId))
                {
                    if (spanId is null ||
                    !VerifyTraceId(Tracer.Instance.ActiveScope.Span.TraceId, dictSpanContext[B3HttpHeaderNames.B3TraceId]))
                    {
                        throw new Exception($"The span context was not injected into the message properly. span-id: {dictSpanContext[B3HttpHeaderNames.B3SpanId]}, trace-id: {dictSpanContext[B3HttpHeaderNames.B3TraceId]}, active trace-id: {Tracer.Instance.ActiveScope.Span.TraceId}");
                    }
                }
                else
                {
                    throw new NotSupportedException("There is no data for Datadog nor B3 propagator");
                }
            }
        }

        private static bool VerifyTraceId(TraceId expectedTraceId, string actualTraceId)
        {
            return VerifyTraceIdByDatadogConvention(expectedTraceId, actualTraceId) ||
                    VerifyTraceIdByOpenTelemetryConvention(expectedTraceId, actualTraceId);
        }

        private static bool VerifyTraceIdByDatadogConvention(TraceId expectedTraceId, string actualTraceId)
        {
            return ulong.TryParse(actualTraceId, out ulong result) && result == expectedTraceId.Lower;
        }

        private static bool VerifyTraceIdByOpenTelemetryConvention(TraceId expectedTraceId, string actualTraceId)
        {
            var traceId = TraceId.CreateFromString(actualTraceId);
            return expectedTraceId == traceId;
        }

        public static void AssertNoDistributedTracingHeaders(List<Message> messages)
        {
            foreach (var message in messages)
            {
                if (message.MessageAttributes.ContainsKey("_datadog"))
                {
                    throw new Exception($"The \"_datadog\" header was found in the message, with value: {message.MessageAttributes["_datadog"].StringValue}");
                }
            }
        }
    }
}
