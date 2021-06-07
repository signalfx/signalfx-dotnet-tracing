// Modified by SignalFx
using System;
using System.Data;
using SignalFx.Tracing;
using SignalFx.Tracing.ExtensionMethods;
using SignalFx.Tracing.Logging;
using SignalFx.Tracing.Util;

namespace Datadog.Trace.ClrProfiler
{
    /// <summary>
    /// Convenience class that creates scopes and populates them with some standard details.
    /// </summary>
    internal static class ScopeFactory
    {
        public const string OperationName = "http.request";

        private static readonly SignalFx.Tracing.Vendors.Serilog.ILogger Log = SignalFxLogging.GetLogger(typeof(ScopeFactory));

        /// <summary>
        /// Gets a scope for outbound HTTP requests if one is the current active scope.
        /// </summary>
        /// <param name="tracer">The tracer instance to use to create the new scope.</param>
        /// <param name="httpMethod">The HTTP method used by the request.</param>
        /// <param name="requestUri">The URI requested by the request.</param>
        /// <returns>
        /// A scope for the outbound HTTP requests if one is current active, null if the
        /// HTTP call was already instrumented or the current scope is not for an HTTP
        /// request.
        /// </returns>
        public static Scope GetActiveHttpScope(Tracer tracer, string httpMethod, Uri requestUri)
        {
            var scope = tracer.ActiveScope;

            var parent = scope?.Span;

            if (parent != null &&
                StringComparer.OrdinalIgnoreCase.Equals(parent.GetTag(Tags.SpanKind), SpanKinds.Client) &&
                StringComparer.OrdinalIgnoreCase.Equals(parent.GetTag(Tags.HttpMethod), httpMethod) &&
                StringComparer.OrdinalIgnoreCase.Equals(parent.GetTag(Tags.HttpUrl), UriHelpers.CleanUri(requestUri, removeScheme: false, tryRemoveIds: false)))
            {
                return scope;
            }

            return null;
        }

        /// <summary>
        /// Creates a span context for outbound http requests, or get the active one.
        /// Used to propagate headers without changing the active span.
        /// </summary>
        /// <param name="tracer">The tracer instance to use to create the span.</param>
        /// <param name="httpMethod">The HTTP method used by the request.</param>
        /// <param name="requestUri">The URI requested by the request.</param>
        /// <param name="integrationName">The name of the integration creating this scope.</param>
        /// <returns>A span context to use to populate headers</returns>
        public static SpanContext CreateHttpSpanContext(
            Tracer tracer,
            string httpMethod,
            Uri requestUri,
            string integrationName)
        {
            if (!tracer.Settings.IsIntegrationEnabled(integrationName))
            {
                // integration disabled, skip this trace
                return null;
            }

            try
            {
                var activeScope = GetActiveHttpScope(tracer, httpMethod, requestUri);

                if (activeScope != null)
                {
                    // This HTTP request was already instrumented return the active HTTP context.
                    return activeScope.Span.Context;
                }

                return tracer.CreateSpanContext(out bool _);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating or populating span context.");
            }

            return null;
        }

        /// <summary>
        /// Creates a scope for outbound http requests and populates some common details.
        /// </summary>
        /// <param name="tracer">The tracer instance to use to create the new scope.</param>
        /// <param name="httpMethod">The HTTP method used by the request.</param>
        /// <param name="requestUri">The URI requested by the request.</param>
        /// <param name="integrationName">The name of the integration creating this scope.</param>
        /// <param name="propagatedSpanId">The propagated span ID.</param>
        /// <returns>A new pre-populated scope.</returns>
        public static Scope CreateOutboundHttpScope(Tracer tracer, string httpMethod, Uri requestUri, string integrationName, ulong? propagatedSpanId = null)
        {
            if (!tracer.Settings.IsIntegrationEnabled(integrationName))
            {
                // integration disabled, don't create a scope, skip this trace
                return null;
            }

            Scope scope = null;

            try
            {
                if (GetActiveHttpScope(tracer, httpMethod, requestUri) != null)
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

                scope = tracer.StartActive(spanName, spanId: propagatedSpanId);
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
