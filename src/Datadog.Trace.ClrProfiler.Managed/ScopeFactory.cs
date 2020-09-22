// Modified by SignalFx
using System;
using System.Data;
using System.Data.Common;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.Logging;
using Datadog.Trace.Util;

namespace Datadog.Trace.ClrProfiler
{
    /// <summary>
    /// Convenience class that creates scopes and populates them with some standard details.
    /// </summary>
    internal static class ScopeFactory
    {
        public const string OperationName = "http.request";

        private static readonly Vendors.Serilog.ILogger Log = DatadogLogging.GetLogger(typeof(ScopeFactory));

        /// <summary>
        /// Creates a scope for outbound http requests and populates some common details.
        /// </summary>
        /// <param name="tracer">The tracer instance to use to create the new scope.</param>
        /// <param name="httpMethod">The HTTP method used by the request.</param>
        /// <param name="requestUri">The URI requested by the request.</param>
        /// <param name="integrationName">The name of the integration creating this scope.</param>
        /// <returns>A new pre-populated scope.</returns>
        public static Scope CreateOutboundHttpScope(Tracer tracer, string httpMethod, Uri requestUri, string integrationName)
        {
            if (!tracer.Settings.IsIntegrationEnabled(integrationName))
            {
                // integration disabled, don't create a scope, skip this trace
                return null;
            }

            Scope scope = null;

            try
            {
                Span parent = tracer.ActiveScope?.Span;

                if (parent != null &&
                    StringComparer.OrdinalIgnoreCase.Equals(parent.GetTag(Tags.SpanKind), SpanKinds.Client) &&
                    StringComparer.OrdinalIgnoreCase.Equals(parent.GetTag(Tags.HttpMethod), httpMethod) &&
                    StringComparer.OrdinalIgnoreCase.Equals(parent.GetTag(Tags.HttpUrl), UriHelpers.CleanUri(requestUri, removeScheme: false, tryRemoveIds: false)))
                {
                    // we are already instrumenting this,
                    // don't instrument nested methods that belong to the same stacktrace
                    // e.g. HttpClientHandler.SendAsync() -> SocketsHttpHandler.SendAsync()
                    return null;
                }

                var spanName = httpMethod ?? OperationName;
                if (tracer.Settings.AppendUrlPathToName)
                {
                    spanName += ":" + requestUri.AbsolutePath;
                }

                scope = tracer.StartActive(spanName);
                var span = scope.Span;

                span.ServiceName = tracer.DefaultServiceName;

                span.SetTag(Tags.SpanKind, SpanKinds.Client);
                // Only the span responsible for propagated context should have client span.kind
                if (span.Context.Parent != null)
                {
                    var parentSpan = ((SpanContext)span.Context.Parent).Span;
                    var spanKind = parentSpan.GetTag(Tags.SpanKind);
                    if (SpanKinds.Client.Equals(spanKind))
                    {
                        parentSpan.SetTag(Tags.SpanKind, null);
                    }
                }

                span.SetTag(Tags.HttpMethod, httpMethod?.ToUpperInvariant());
                span.SetTag(Tags.HttpUrl, UriHelpers.CleanUri(requestUri, removeScheme: false, tryRemoveIds: false));
                span.SetTag(Tags.InstrumentationName, integrationName);

                // set analytics sample rate if enabled
                var analyticsSampleRate = tracer.Settings.GetIntegrationAnalyticsSampleRate(integrationName, enabledWithGlobalSetting: false);
                span.SetMetric(Tags.Analytics, analyticsSampleRate);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating or populating scope.");
            }

            // always returns the scope, even if it's null because we couldn't create it,
            // or we couldn't populate it completely (some tags is better than no tags)
            return scope;
        }

        public static Scope CreateDbCommandScope(Tracer tracer, IDbCommand command, string integrationName)
        {
            if (!tracer.Settings.IsIntegrationEnabled(integrationName))
            {
                // integration disabled, don't create a scope, skip this trace
                return null;
            }

            Scope scope = null;

            try
            {
                string dbType = GetDbType(command.GetType().Name);

                if (dbType == null)
                {
                    // don't create a scope, skip this trace
                    return null;
                }

                Span parent = tracer.ActiveScope?.Span;

                string statement = command.CommandText;
                if (parent != null &&
                    parent.GetTag(Tags.DbType) == dbType &&
                    parent.GetTag(Tags.DbStatement) == statement)
                {
                    // we are already instrumenting this,
                    // don't instrument nested methods that belong to the same stacktrace
                    // e.g. ExecuteReader() -> ExecuteReader(commandBehavior)
                    return null;
                }

                string operationName = $"{dbType}.query";

                scope = tracer.StartActive(operationName, serviceName: tracer.DefaultServiceName);
                var span = scope.Span;
                span.SetTag(Tags.DbType, dbType);
                span.SetTag(Tags.InstrumentationName, integrationName);
                span.AddTagsFromDbCommand(command, statement);

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

        public static string GetDbType(string commandTypeName)
        {
            switch (commandTypeName)
            {
                case "SqlCommand":
                    return "sql-server";
                case "NpgsqlCommand":
                    return "postgres";
                case "InterceptableDbCommand":
                case "ProfiledDbCommand":
                    // don't create spans for these
                    return null;
                default:
                    const string commandSuffix = "Command";

                    // remove "Command" suffix if present
                    return commandTypeName.EndsWith(commandSuffix)
                               ? commandTypeName.Substring(0, commandTypeName.Length - commandSuffix.Length).ToLowerInvariant()
                               : commandTypeName.ToLowerInvariant();
            }
        }
    }
}
