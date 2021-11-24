using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                    !ulong.TryParse(dictSpanContext[DDHttpHeaderNames.TraceId], out ulong result) ||
                    result != Tracer.Instance.ActiveScope.Span.TraceId.Lower)
                    {
                        throw new Exception($"The span context was not injected into the message properly. parent-id: {dictSpanContext[DDHttpHeaderNames.ParentId]}, trace-id: {dictSpanContext[DDHttpHeaderNames.TraceId]}, active trace-id: {Tracer.Instance.ActiveScope.Span.TraceId}");
                    }
                }
                else if (dictSpanContext.TryGetValue(B3HttpHeaderNames.B3ParentId, out var parentIdB3))
                {
                    if (parentIdB3 is null ||
                    !ulong.TryParse(dictSpanContext[B3HttpHeaderNames.B3TraceId], out ulong resultB3) ||
                    resultB3 != Tracer.Instance.ActiveScope.Span.TraceId.Lower)
                    {
                        throw new Exception($"The span context was not injected into the message properly. parent-id: {dictSpanContext[B3HttpHeaderNames.B3ParentId]}, trace-id: {dictSpanContext[B3HttpHeaderNames.B3TraceId]}, active trace-id: {Tracer.Instance.ActiveScope.Span.TraceId}");
                    }
                }
                else
                {
                    throw new NotSupportedException("There is no data for Datadog nor B3 propagator");
                }
            }
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
