// Modified by SignalFx
using System;
using System.Data;
using System.Data.Common;
using System.Net;
using System.Net.Sockets;

namespace SignalFx.Tracing.ExtensionMethods
{
    /// <summary>
    /// Extension methods for the <see cref="Span"/> class.
    /// </summary>
    public static class SpanExtensions
    {
        /// <summary>
        /// Sets the sampling priority for the trace that contains the specified <see cref="Span"/>.
        /// </summary>
        /// <param name="span">A span that belongs to the trace.</param>
        /// <param name="samplingPriority">The new sampling priority for the trace.</param>
        public static void SetTraceSamplingPriority(this Span span, SamplingPriority samplingPriority)
        {
            if (span == null) { throw new ArgumentNullException(nameof(span)); }

            if (span.Context.TraceContext != null)
            {
                span.Context.TraceContext.SamplingPriority = samplingPriority;
            }
        }

        /// <summary>
        /// Adds standard tags to a span with values taken from the specified <see cref="DbCommand"/>.
        /// </summary>
        /// <param name="span">The span to add the tags to.</param>
        /// <param name="command">The db command to get tags values from.</param>
        /// <param name="statement">The db statement to use over command.CommandText. Will not be truncated or sanitized.</param>
        public static void AddTagsFromDbCommand(this Span span, IDbCommand command, string statement = "")
        {
            if (string.IsNullOrEmpty(statement))
            {
                statement = command.CommandText ?? string.Empty;
            }

            if (!string.IsNullOrEmpty(statement))
            {
                span.SetTag(Tags.DbStatement, statement);
            }

            span.SetTag(Tags.SpanKind, SpanKinds.Client);

            // parse the connection string
            var builder = new DbConnectionStringBuilder { ConnectionString = command.Connection.ConnectionString };

            string database = GetConnectionStringValue(builder, "Database", "Initial Catalog", "InitialCatalog");
            span.SetTag(Tags.DbName, database);

            string user = GetConnectionStringValue(builder, "User ID", "UserID");
            span.SetTag(Tags.DbUser, user);

            string server = GetConnectionStringValue(builder, "Server", "Data Source", "DataSource", "Network Address", "NetworkAddress", "Address", "Addr", "Host");
            span.SetTag(Tags.OutHost, server);
        }

        internal static void DecorateWebServerSpan(
            this Span span,
            string resourceName,
            string method,
            string host,
            string httpUrl,
            IPAddress remoteIp = null)
        {
            span.Type = SpanTypes.Web;
            span.ResourceName = resourceName?.Trim();
            if (Tracer.Instance.Settings.UseWebServerResourceAsOperationName && !string.IsNullOrEmpty(span.ResourceName))
            {
                span.OperationName = span.ResourceName;
            }

            span.SetTag(Tags.SpanKind, SpanKinds.Server);
            span.SetTag(Tags.HttpMethod, method);
            span.SetTag(Tags.HttpRequestHeadersHost, host);
            span.SetTag(Tags.HttpUrl, httpUrl);

            switch (remoteIp?.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    span.SetTag(Tags.PeerIpV4, remoteIp.ToString());
                    break;
                case AddressFamily.InterNetworkV6:
                    span.SetTag(Tags.PeerIpV6, remoteIp.ToString());
                    break;
            }
        }

        private static string GetConnectionStringValue(DbConnectionStringBuilder builder, params string[] names)
        {
            foreach (string name in names)
            {
                if (builder.TryGetValue(name, out object valueObj) &&
                    valueObj is string value)
                {
                    return value;
                }
            }

            return null;
        }
    }
}
