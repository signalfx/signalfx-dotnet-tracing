// Modified by SignalFx
namespace Datadog.Trace
{
    internal static class TracerConstants
    {
        public const string Language = "dotnet-tracing";

        /// <summary>
        /// 2^60-1, the trace ID comes from a GUID but it has a fixed part, the fixed part is removed
        /// to avoid any bias on sampling.
        /// </summary>
        public const ulong MaxTraceId = SpanContext.RandomIdBitMask;

        public static readonly string AssemblyVersion = typeof(Tracer).Assembly.GetName().Version.ToString();
    }
}
