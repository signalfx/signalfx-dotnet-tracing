// Modified by SignalFx
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SignalFx.Tracing;
using SignalFx.Tracing.ExtensionMethods;
using Xunit;

namespace Datadog.Trace.TestHelpers
{
    /// <summary>
    /// This class provides helpers to read Span and ServiceInfo data
    /// from serialized Zipkin representation.  Modeled from MsgPackHelpers.
    /// </summary>
    public static class ZipkinHelpers
    {
        public static ulong TraceId(this JToken obj)
        {
            return Convert.ToUInt64(obj.FirstDictionary()["traceId"].ToString(), 16);
        }

        public static ulong SpanId(this JToken obj)
        {
            return Convert.ToUInt64(obj.FirstDictionary()["id"].ToString(), 16);
        }

        public static ulong ParentId(this JToken obj)
        {
            return Convert.ToUInt64(obj.FirstDictionary()["parentId"].ToString(), 16);
        }

        public static string OperationName(this JToken obj)
        {
            return obj.FirstDictionary()["name"].ToString();
        }

        public static string ResourceName(this JToken obj)
        {
            string value;
            return Tags(obj).TryGetValue(SignalFx.Tracing.Tags.ResourceName, out value) ? value : OperationName(obj);
        }

        public static string ServiceName(this JToken obj)
        {
            var localEndpoint = obj.FirstDictionary()["localEndpoint"];
            return localEndpoint["serviceName"].ToString();
        }

        public static long StartTime(this JToken obj)
        {
            return (long)obj.FirstDictionary()["timestamp"];
        }

        public static long Duration(this JToken obj)
        {
            return (long)obj.FirstDictionary()["duration"];
        }

        public static string Type(this JToken obj)
        {
            string value;
            return Tags(obj).TryGetValue(SignalFx.Tracing.Tags.SpanType, out value) ? value : null;
        }

        public static bool Error(this JToken obj)
        {
            string value;
            return Tags(obj).TryGetValue("error", out value) ? value == "true" : false;
        }

        public static Dictionary<string, string> Tags(this JToken obj)
        {
            return obj.FirstDictionary()["tags"].ToObject<Dictionary<string, string>>();
        }

        public static Dictionary<DateTimeOffset, Dictionary<string, string>> Logs(this JToken obj)
        {
            var annotations = obj.FirstDictionary()["annotations"].ToObject<List<Dictionary<string, object>>>();
            var logs = new Dictionary<DateTimeOffset, Dictionary<string, string>>();
            foreach (var item in annotations)
            {
                // Zipkin timestamps are in µS
                var timestamp = TimeHelpers.UnixMicrosecondsToDateTimeOffset((long)item["timestamp"]);
                var fields = JsonConvert.DeserializeObject<Dictionary<string, string>>(item["value"].ToString());
                logs[timestamp] = fields;
            }

            return logs;
        }

        public static void AssertSpanEqual(Span expected, JToken actual, string expectedServiceName = null)
        {
            Assert.Equal(expected.Context.TraceId, actual.TraceId());
            Assert.Equal(expected.Context.SpanId, actual.SpanId());
            if (expected.Context.ParentId.HasValue)
            {
                Assert.Equal(expected.Context.ParentId, actual.ParentId());
            }

            Assert.Equal(expected.OperationName, actual.OperationName());
            Assert.Equal(expected.ResourceName, actual.ResourceName());
            Assert.Equal(expectedServiceName ?? Tracer.Instance.DefaultServiceName, actual.ServiceName());
            Assert.Equal(expected.Type, actual.Type());
            Assert.Equal(expected.StartTime.ToUnixTimeMicroseconds(), actual.StartTime());
            Assert.Equal(expected.Duration.ToMicroseconds(), actual.Duration());
            if (expected.Error)
            {
                Assert.True(actual.Error());
            }

            if (expected.Tags != null)
            {
                var actualTags = new Dictionary<string, string>(actual.Tags());
                var expectedTags = new Dictionary<string, string>(expected.Tags);
                actualTags.Remove("span.type");
                actualTags.Remove("resource.name");
                actualTags.Remove("error");

                var expectedSpanKind = DictionaryExtensions.GetValueOrDefault(expectedTags, SignalFx.Tracing.Tags.SpanKind);
                if (expectedSpanKind != null)
                {
                    var actualSpanKind = actual.FirstDictionary()["kind"];
                    Assert.Equal(expectedSpanKind.ToUpper(), actualSpanKind);
                    expectedTags.Remove(SignalFx.Tracing.Tags.SpanKind);
                }

                Assert.Equal(expectedTags, actualTags);
            }

            if (expected.Logs != null)
            {
                var actualLogs = actual.Logs();
                foreach (var item in expected.Logs)
                {
                    var truncatedTimestamp = TimeHelpers.UnixMicrosecondsToDateTimeOffset(item.Key.ToUnixTimeMicroseconds());
                    Assert.Equal(item.Value, actualLogs[truncatedTimestamp]);
                }
            }
        }

        private static JToken FirstDictionary(this JToken obj)
        {
            if (obj is JArray)
            {
                return obj.ToList().First().FirstDictionary();
            }

            return obj;
        }
    }
}
