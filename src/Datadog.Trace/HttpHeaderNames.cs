// Modified by SignalFx
namespace SignalFx.Tracing
{
    /// <summary>
    /// Names of HTTP headers that can be used tracing inbound or outbound HTTP requests.
    /// </summary>
    public static class HttpHeaderNames
    {
        /// <summary>
        /// If header is set to "false", tracing is disabled for that http request.
        /// Tracing is enabled by default.
        /// </summary>
        public const string TracingEnabled = "x-sfx-tracing-enabled";
    }
}
