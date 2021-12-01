// <copyright file="ElasticsearchNetCommon.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Datadog.Trace.ClrProfiler.AutoInstrumentation.Elasticsearch;
using Datadog.Trace.ClrProfiler.Emit;
using Datadog.Trace.Configuration;
using Datadog.Trace.Logging;

namespace Datadog.Trace.ClrProfiler.Integrations
{
    internal static class ElasticsearchNetCommon
    {
        public const string OperationName = "elasticsearch.query";
        public const string ServiceName = "elasticsearch";
        public const string SpanType = "elasticsearch";
        public const string ComponentValue = "elasticsearch-net";
        public static readonly Type CancellationTokenType = typeof(CancellationToken);
        public static readonly Type RequestPipelineType = Type.GetType("Elasticsearch.Net.IRequestPipeline, Elasticsearch.Net");

        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(ElasticsearchNetCommon));

        public static Scope CreateScope<T>(Tracer tracer, IntegrationInfo integrationId, RequestPipelineStruct pipeline, T requestData)
            where T : IRequestData
        {
            if (!tracer.Settings.IsIntegrationEnabled(integrationId))
            {
                // integration disabled, don't create a scope, skip this trace
                return null;
            }

            var pathAndQuery = requestData.Path;
            string method = requestData.Method;
            var url = requestData.Uri?.ToString();

            var scope = CreateScope(tracer, integrationId, pathAndQuery, method, pipeline.RequestParameters, out var tags);
            tags.Url = url;
            return scope;
        }

        public static Scope CreateScope(Tracer tracer, IntegrationInfo integrationId, string path, string method, object requestParameters, out ElasticsearchTags tags)
        {
            if (!tracer.Settings.IsIntegrationEnabled(integrationId))
            {
                // integration disabled, don't create a scope, skip this trace
                tags = null;
                return null;
            }

            string requestName = requestParameters?.GetType().Name.Replace("RequestParameters", string.Empty);
            string serviceName = tracer.Settings.GetServiceName(tracer, ServiceName);

            Scope scope = null;

            tags = new ElasticsearchTags();

            try
            {
                var operationName = requestName ?? OperationName;
                scope = tracer.StartActiveWithTags(operationName, serviceName: serviceName, tags: tags);
                var span = scope.Span;
                span.LogicScope = OperationName;
                span.ResourceName = requestName ?? path ?? string.Empty;
                span.Type = SpanType;
                tags.Action = requestName;
                tags.Method = method;

                tags.SetAnalyticsSampleRate(integrationId, tracer.Settings, enabledWithGlobalSetting: false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating or populating scope.");
            }

            return scope;
        }

        /// <summary>
        /// Will attempt to set the db.statement tag from RequestData.PostData, if applicable. In cases where
        /// direct streaming is enabled, this will involve using the PostData object to synchronously write
        /// its content to a byte array.
        /// </summary>
        /// <param name="scope">The scope to tag.</param>
        /// <param name="requestData">The RequestData from which to retrieve the desired PostData content.</param>
        public static void SetDbStatementFromRequestData(this Scope scope, object requestData)
        {
            if (!Tracer.Instance.Settings.TagElasticsearchQueries || scope?.Span == null)
            {
                return;
            }

            if (!ShouldAttemptWrittenBytes(requestData, out var postData, out var writtenBytes))
            {
                return;
            }

            if (writtenBytes == null)
            {
                var methodInfo = GetWriteMethodInfo(postData, requestData, out var connectionSettings);
                using (var stream = new MemoryStream())
                {
                    var args = new object[] { stream, connectionSettings };
                    methodInfo.Invoke(postData, args);
                    writtenBytes = stream.ToArray();
                }
            }

            SetDbStatement(scope.Span, writtenBytes);
        }

        /// <summary>
        /// Attempts to load the PostData.WrittenBytes property from Elasticsearch.Net.RequestData.
        /// This will return false if tagging Elasticsearch queries is disabled or if data isn't applicable
        /// for the http method (no PostData was supplied to the request), and true otherwise. It will
        /// also obtain the PostData object in case manually writing the data is necessary (direct streaming enabled).
        /// </summary>
        /// <param name="requestData">The request data.</param>
        /// <param name="postData">The PostData property the request data.</param>
        /// <param name="writtenBytes">The WrittenBytes property of the PostData.</param>
        /// <returns>Whether the request is appplicable for retrieving PostData content.</returns>
        private static bool ShouldAttemptWrittenBytes(object requestData, out object postData, out byte[] writtenBytes)
        {
            writtenBytes = null;

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
        /// Will set the db.statement tag with truncated post data.
        /// </summary>
        /// <param name="span">The Span to tag.</param>
        /// <param name="writtenBytes">The byte[] of data with which to encode, truncate, and tag.</param>
        private static void SetDbStatement(Span span, byte[] writtenBytes)
        {
            if (span == null || writtenBytes == null)
            {
                return;
            }

            // the buffer size is set arbitrary
            const int MaxBufferSize = 1024;

            string postData = null;
            if (writtenBytes.Length > MaxBufferSize)
            {
                postData = System.Text.Encoding.UTF8.GetString(writtenBytes, 0, MaxBufferSize);
            }
            else
            {
                postData = System.Text.Encoding.UTF8.GetString(writtenBytes);
            }

            span.SetTag(Tags.DbStatement, postData);
        }

        /// <summary>
        /// Will get the Write MethodInfo used to obtain the PostData's content when
        /// WrittenBytes property is null (direct streaming enabled). Also obtains the RequestData
        /// ConnectionSettings used by these methods.
        /// </summary>
        /// <param name="postData">The PostData from which to retrieve the desired MethodInfo.</param>
        /// <param name="requestData">The RequestData from which to retrieve the ConnectionSettings used by the write method.</param>
        /// <param name="connectionSettings">The RequestData.ConnectionSettings property.</param>
        /// <returns>The Write or WriteAsync MethodInfo.</returns>
        private static MethodInfo GetWriteMethodInfo(object postData, object requestData, out object connectionSettings)
        {
            connectionSettings = requestData.GetProperty("ConnectionSettings").GetValueOrDefault();
            var postDataType = postData.GetType();
            return postDataType.GetMethod("Write");
        }
    }
}
