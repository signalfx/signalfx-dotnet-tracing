// Modified by SignalFx
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Datadog.Trace.ClrProfiler.Emit;
using Datadog.Trace.Logging;

namespace Datadog.Trace.ClrProfiler.Integrations
{
    internal static class ElasticsearchNetCommon
    {
        public const string OperationName = "elasticsearch.query";
        public const string ServiceName = "elasticsearch";
        public const string SpanType = "elasticsearch";
        public const string ComponentValue = "elasticsearch-net";
        public const string ElasticsearchActionKey = "elasticsearch.action";
        public const string ElasticsearchMethodKey = "elasticsearch.method";
        public const string ElasticsearchUrlKey = "elasticsearch.url";
        public const string Replacement = "?";

        public static readonly List<Regex> SanitizePatterns = new List<Regex>() { new Regex("\"username\":\\s*\"([^\"]*)\""), new Regex("\"password\":\\s*\"([^\"]*)\"") };
        public static readonly Type CancellationTokenType = typeof(CancellationToken);
        public static readonly Type RequestPipelineType = Type.GetType("Elasticsearch.Net.IRequestPipeline, Elasticsearch.Net");
        public static readonly Type RequestDataType = Type.GetType("Elasticsearch.Net.RequestData, Elasticsearch.Net");

        private static readonly Vendors.Serilog.ILogger Log = DatadogLogging.GetLogger(typeof(ElasticsearchNetCommon));

        public static Scope CreateScope(Tracer tracer, string integrationName, object pipeline, object requestData)
        {
            if (!tracer.Settings.IsIntegrationEnabled(integrationName))
            {
                // integration disabled, don't create a scope, skip this trace
                return null;
            }

            string requestName = pipeline.GetProperty("RequestParameters")
                                         .GetValueOrDefault()
                                        ?.GetType()
                                         .Name
                                         .Replace("RequestParameters", string.Empty);

            var pathAndQuery = requestData.GetProperty<string>("PathAndQuery").GetValueOrDefault() ??
                               requestData.GetProperty<string>("Path").GetValueOrDefault();

            string method = requestData.GetProperty("Method").GetValueOrDefault()?.ToString();
            var url = requestData.GetProperty("Uri").GetValueOrDefault()?.ToString();

            var serviceName = string.Join("-", tracer.DefaultServiceName, ServiceName);

            Scope scope = null;

            try
            {
                var operationName = requestName ?? OperationName;
                scope = tracer.StartActive(operationName, serviceName: serviceName);
                var span = scope.Span;
                span.SetTag(Tags.InstrumentationName, ComponentValue);
                span.SetTag(Tags.DbType, SpanType);
                span.SetTag(Tags.SpanKind, SpanKinds.Client);
                span.SetTag(ElasticsearchMethodKey, method);
                span.SetTag(ElasticsearchUrlKey, url);

                // set analytics sample rate if enabled
                var analyticsSampleRate = tracer.Settings.GetIntegrationAnalyticsSampleRate(integrationName, enabledWithGlobalSetting: false);
                span.SetMetric(Tags.Analytics, analyticsSampleRate);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating or populating scope.");
            }

            return scope;
        }

        public static bool AttemptWrittenBytes(Span span, object requestData, out object postData, out byte[] writtenBytes)
        {
            postData = null;
            writtenBytes = null;
            if (span == null)
            {
                return false;
            }

            if (!Tracer.Instance.Settings.TagElasticsearchQueries)
            {
                return false;
            }

            postData = requestData.GetProperty("PostData")
                                  .GetValueOrDefault();
            if (postData == null)
            {
                return false;
            }

            writtenBytes = (byte[])postData.GetProperty("WrittenBytes").GetValueOrDefault();
            return true;
        }

        public static MethodInfo GetWriteMethodInfo(string methodName, object requestData, object postData, out object connectionSettings)
        {
            connectionSettings = requestData.GetProperty("ConnectionSettings").GetValueOrDefault();
            var postDataType = postData.GetType();
            return postDataType.GetMethod(methodName);
        }

        public static string SanitizePostData(string data)
        {
            foreach (var pattern in SanitizePatterns)
            {
                data = pattern.Replace(data, (m) =>
                {
                    var group = m.Groups[1];
                    var start = m.Value.Substring(0, group.Index - m.Index);
                    var finish = m.Value.Substring(group.Index - m.Index + group.Length);
                    return string.Format("{0}{1}{2}", start, Replacement, finish);
                });
            }

            return data;
        }

        public static void SetDbStatement(Span span, byte[] writtenBytes)
        {
            string postData = null;
            if (writtenBytes == null)
            {
                return;
            }

            if (writtenBytes.Length > 1024)
            {
                postData = System.Text.Encoding.UTF8.GetString(writtenBytes, 0, 1024);
            }
            else
            {
                postData = System.Text.Encoding.UTF8.GetString(writtenBytes);
            }

            string statement = SanitizePostData(postData);
            span.SetTag(Tags.DbStatement, statement);
        }

        public static void SetDbStatementFromRequestData(this Span span, object requestData)
        {
            object postData;
            byte[] writtenBytes;
            if (!AttemptWrittenBytes(span, requestData, out postData, out writtenBytes))
            {
                return;
            }

            if (writtenBytes == null)
            {
                object connectionSettings;
                var methodInfo = GetWriteMethodInfo("Write", requestData, postData, out connectionSettings);
                using (var stream = new MemoryStream())
                {
                    object[] args = new object[] { stream, connectionSettings };
                    methodInfo.Invoke(postData, args);
                    writtenBytes = stream.ToArray();
                }
            }

            SetDbStatement(span, writtenBytes);
        }

        public static async Task SetDbStatementFromRequestDataAsync(this Span span, object requestData)
        {
            object postData;
            byte[] writtenBytes;
            if (!AttemptWrittenBytes(span, requestData, out postData, out writtenBytes))
            {
                return;
            }

            if (writtenBytes == null)
            {
                object connectionSettings;
                var methodInfo = GetWriteMethodInfo("WriteAsync", requestData, postData, out connectionSettings);
                using (var stream = new MemoryStream())
                {
                    object[] args = new object[] { stream, connectionSettings, null };
                    await (Task)(methodInfo.Invoke(postData, args));
                    writtenBytes = stream.ToArray();
                }
            }

            SetDbStatement(span, writtenBytes);
        }
    }
}
