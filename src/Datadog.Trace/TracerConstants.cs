// Modified by SignalFx
namespace SignalFx.Tracing
{
    internal static class TracerConstants
    {
        public const string Language = "dotnet-tracing";

        /// <summary>
        /// 2^64.
        /// </summary>
        public const ulong MaxTraceId = ulong.MaxValue;

        public static readonly string AssemblyVersion = typeof(Tracer).Assembly.GetName().Version.ToString();
    }
}
