// <copyright file="ElasticsearchNetCommon.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;
using System.IO;
using System.Threading;
using Datadog.Trace.Configuration;
using Datadog.Trace.DuckTyping;
using Datadog.Trace.Logging;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Elasticsearch
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

        public static Scope CreateScope<T>(Tracer tracer, IntegrationId integrationId, RequestPipelineStruct pipeline, T requestData)
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

        public static Scope CreateScope(Tracer tracer, IntegrationId integrationId, string path, string method, object requestParameters, out ElasticsearchTags tags)
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
                scope = tracer.StartActiveInternal(operationName, serviceName: serviceName, tags: tags);
                var span = scope.Span;
                span.LogicScope = OperationName;
                span.ResourceName = requestName ?? path ?? string.Empty;
                span.Type = SpanType;
                tags.Action = requestName;
                tags.Method = method;

                tags.SetAnalyticsSampleRate(integrationId, tracer.Settings, enabledWithGlobalSetting: false);
                tracer.TracerManager.Telemetry.IntegrationGeneratedSpan(integrationId);
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
        public static void SetDbStatementFromRequestData(this Scope scope, IRequestData requestData)
        {
            if (!Tracer.Instance.Settings.TagElasticsearchQueries || scope?.Span == null)
            {
                return;
            }

            var postData = requestData?.PostData;
            if (postData == null)
            {
                return;
            }

            var writtenBytes = postData.WrittenBytes;

            if (writtenBytes == null)
            {
                using (var stream = new MemoryStream())
                {
                    postData.Write(stream, requestData.ConnectionSettings);
                    writtenBytes = stream.ToArray();
                }
            }

            SetDbStatement(scope.Span, writtenBytes);
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
    }
}
