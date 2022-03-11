// <copyright file="SpanExtensions.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Datadog.Trace.Configuration;
using Datadog.Trace.Headers;
using Datadog.Trace.Propagation;
using Datadog.Trace.Tagging;
using Datadog.Trace.Util;
using Datadog.Trace.Vendors.Serilog;

namespace Datadog.Trace.ExtensionMethods
{
    /// <summary>
    /// Extension methods for the <see cref="ISpan"/> class.
    /// </summary>
    public static class SpanExtensions
    {
        /// <summary>
        /// Adds standard tags to a span with values taken from the specified <see cref="DbCommand"/>.
        /// </summary>
        /// <param name="span">The span to add the tags to.</param>
        /// <param name="command">The db command to get tags values from.</param>
        public static void AddTagsFromDbCommand(this ISpan span, IDbCommand command)
        {
            var commandText = command.CommandText;
            span.ResourceName = commandText;
            span.Type = SpanTypes.Sql;

            span.SetTag(Tags.DbStatement, commandText);

            var tag = DbCommandCache.GetTagsFromDbCommand(command);

            span.SetTag(Tags.DbName, tag.DbName);
            span.SetTag(Tags.DbUser, tag.DbUser);
            span.SetTag(Tags.OutHost, tag.OutHost);
        }

        /// <summary>
        /// Sets the sampling priority for the trace that contains the specified <see cref="ISpan"/>.
        /// </summary>
        /// <param name="span">A span that belongs to the trace.</param>
        /// <param name="samplingPriority">The new sampling priority for the trace.</param>
        public static void SetTraceSamplingPriority(this ISpan span, SamplingPriority samplingPriority)
        {
            span.SetTraceSamplingPriority((int)samplingPriority);
        }

        internal static void SetTraceSamplingPriority(this ISpan span, int samplingPriority)
        {
            if (span == null) { ThrowHelper.ThrowArgumentNullException(nameof(span)); }

            if (span.Context is SpanContext spanContext && spanContext.TraceContext != null)
            {
                spanContext.TraceContext.SetSamplingPriority(samplingPriority);
            }
        }

        internal static void DecorateWebServerSpan(
            this ISpan span,
            string resourceName,
            string method,
            string host,
            string httpUrl,
            WebTags tags,
            IEnumerable<KeyValuePair<string, string>> tagsFromHeaders,
            string remoteIp = null)
        {
            span.Type = SpanTypes.Web;
            span.ResourceName = resourceName?.Trim();

            if (tags is not null)
            {
                tags.HttpMethod = method;
                tags.HttpRequestHeadersHost = host;
                tags.HttpUrl = httpUrl;
                tags.PeerIp = remoteIp;
            }

            foreach (KeyValuePair<string, string> kvp in tagsFromHeaders)
            {
                span.SetTag(kvp.Key, kvp.Value);
            }
        }

        internal static void SetHeaderTags<T>(this ISpan span, T headers, IReadOnlyDictionary<string, string> headerTags, string defaultTagPrefix)
            where T : IHeadersCollection
        {
            if (headerTags is not null && !headerTags.IsEmpty())
            {
                try
                {
                    var tagsFromHeaders = headers.ExtractHeaderTags(headerTags, defaultTagPrefix);
                    foreach (KeyValuePair<string, string> kvp in tagsFromHeaders)
                    {
                        span.SetTag(kvp.Key, kvp.Value);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error extracting propagated HTTP headers.");
                }
            }
        }

        internal static bool HasHttpStatusCode(this Span span)
        {
            if (span.Tags is IHasStatusCode statusCodeTags)
            {
                return statusCodeTags.HttpStatusCode is not null;
            }
            else
            {
                return span.GetTag(Tags.HttpStatusCode) is not null;
            }
        }

        internal static void SetHttpStatusCode(this Span span, int statusCode, bool isServer, ImmutableTracerSettings tracerSettings)
        {
            string statusCodeString = ConvertStatusCodeToString(statusCode);

            if (span.Tags is IHasStatusCode statusCodeTags)
            {
                statusCodeTags.HttpStatusCode = statusCodeString;
            }
            else
            {
                span.SetTag(Tags.HttpStatusCode, statusCodeString);
            }

            // Check the customers http statuses that should be marked as errors
            if (tracerSettings.IsErrorStatusCode(statusCode, isServer))
            {
                // if an error message already exists (e.g. from a previous exception), don't replace it
                if (string.IsNullOrEmpty(span.GetTag(Tags.ErrorMsg)))
                {
                    var message = $"The HTTP response has status code {statusCodeString}.";
                    span.Status = SpanStatus.Error;
                    span.SetTag(Tags.ErrorMsg, message);
                }
                else
                {
                    span.Status = SpanStatus.Error;
                }
            }
        }

        private static string ConvertStatusCodeToString(int statusCode)
        {
            if (statusCode == 200)
            {
                return "200";
            }

            if (statusCode == 302)
            {
                return "302";
            }

            if (statusCode == 401)
            {
                return "401";
            }

            if (statusCode == 403)
            {
                return "403";
            }

            if (statusCode == 404)
            {
                return "404";
            }

            if (statusCode == 500)
            {
                return "500";
            }

            if (statusCode == 503)
            {
                return "503";
            }

            return statusCode.ToString();
        }
    }
}
