// Modified by SignalFx
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Datadog.Trace.ClrProfiler.Emit;
using SignalFx.Tracing;
using SignalFx.Tracing.Logging;

namespace Datadog.Trace.ClrProfiler.Integrations
{
    internal static class ElasticsearchNetCommon
    {
        public const string OperationName = "elasticsearch.query";
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

        private static readonly SignalFx.Tracing.Vendors.Serilog.ILogger Log = SignalFxLogging.GetLogger(typeof(ElasticsearchNetCommon));

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

            Scope scope = null;

            try
            {
                var operationName = requestName ?? OperationName;
                scope = tracer.StartActive(operationName, serviceName: tracer.DefaultServiceName);
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

        /// <summary>
        /// Attempts to load the PostData.WrittenBytes property from Elasticsearch.Net.RequestData.
        /// This will return false if tagging Elasticsearch queries is disabled or if data isn't applicable
        /// for the http method (no PostData was supplied to the request), and true otherwise.  It will
        /// also obtain the PostData object in case manually writing the data is necessary (direct streaming enabled).
        /// </summary>
        /// <param name="requestData">The request data.</param>
        /// <param name="postData">The PostData property the request data.</param>
        /// <param name="writtenBytes">The WrittenBytes property of the PostData.</param>
        /// <returns>Whether the request is appplicable for retrieving PostData content.</returns>
        public static bool ShouldAttemptWrittenBytes(object requestData, out object postData, out byte[] writtenBytes)
        {
            postData = null;
            writtenBytes = null;
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

        /// <summary>
        /// Will replace all SanitizePatterns with Replacement, if any matches.
        /// Used to prevent leaking sensitive information.
        /// </summary>
        /// <param name="data">The data to sanitize.</param>
        /// <returns>The sanitized data.</returns>
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

        /// <summary>
        /// Will set the db.statement tag with truncated, sanitized post data.
        /// </summary>
        /// <param name="span">The Span to tag.</param>
        /// <param name="writtenBytes">The byte[] of data with which to encode, sanitize, truncate, and tag.</param>
        public static void SetDbStatement(Span span, byte[] writtenBytes)
        {
            if (span == null || writtenBytes == null)
            {
                return;
            }

            string postData = null;
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

        /// <summary>
        /// Will get the Write or WriteAsync MethodInfo used to obtain the PostData's content when
        /// WrittenBytes property is null (direct streaming enabled).  Also obtains the RequestData
        /// ConnectionSettings used by these methods.
        /// </summary>
        /// <param name="methodName">The method to retrieve from PostData.</param>
        /// <param name="postData">The PostData from which to retrieve the desired MethodInfo.</param>
        /// <param name="requestData">The RequestData from which to retrieve the ConnectionSettings used by the write method.</param>
        /// <param name="connectionSettings">The RequestData.ConnectionSettings property.</param>
        /// <returns>The Write or WriteAsync MethodInfo.</returns>
        public static MethodInfo GetWriteMethodInfo(string methodName, object postData, object requestData, out object connectionSettings)
        {
            connectionSettings = requestData.GetProperty("ConnectionSettings").GetValueOrDefault();
            var postDataType = postData.GetType();
            return postDataType.GetMethod(methodName);
        }

        /// <summary>
        /// Will attempt to set the db.statement tag from RequestData.PostData, if applicable.  In cases where
        /// direct streaming is enabled, this will involve using the PostData object to synchronously write
        /// its content to a byte array.
        /// </summary>
        /// <param name="span">The Span to tag.</param>
        /// <param name="requestData">The RequestData from which to retrieve the desired PostData content.</param>
        public static void SetDbStatementFromRequestData(this Span span, object requestData)
        {
            if (span == null)
            {
                return;
            }

            object postData;
            byte[] writtenBytes;
            if (!ShouldAttemptWrittenBytes(requestData, out postData, out writtenBytes))
            {
                return;
            }

            if (writtenBytes == null)
            {
                object connectionSettings;
                var methodInfo = GetWriteMethodInfo("Write", postData, requestData, out connectionSettings);
                using (var stream = new MemoryStream())
                {
                    object[] args = new object[] { stream, connectionSettings };
                    methodInfo.Invoke(postData, args);
                    writtenBytes = stream.ToArray();
                }
            }

            SetDbStatement(span, writtenBytes);
        }

        /// <summary>
        /// Will attempt to set the db.statement tag from RequestData.PostData, if applicable.  In cases where
        /// direct streaming is enabled, this will involve using the PostData object to asynchronously write
        /// its content to a byte array.
        /// </summary>
        /// <param name="span">The Span to tag.</param>
        /// <param name="requestData">The RequestData from which to retrieve the desired PostData content.</param>
        /// <returns>the WriteAsync Task, if any</returns>
        public static async Task SetDbStatementFromRequestDataAsync(this Span span, object requestData)
        {
            if (span == null)
            {
                return;
            }

            object postData;
            byte[] writtenBytes;
            if (!ShouldAttemptWrittenBytes(requestData, out postData, out writtenBytes))
            {
                return;
            }

            if (writtenBytes == null)
            {
                object connectionSettings;
                var methodInfo = GetWriteMethodInfo("WriteAsync", postData, requestData, out connectionSettings);
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
