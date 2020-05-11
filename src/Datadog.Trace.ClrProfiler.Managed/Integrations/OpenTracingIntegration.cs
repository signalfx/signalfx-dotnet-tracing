// Modified by SignalFx
using Datadog.Trace.ClrProfiler.Helpers;

namespace Datadog.Trace.ClrProfiler.Integrations
{
    /// <summary>
    /// Tracing integration for custom instrumentation using just OpenTracing library.
    /// Acts as a noop to trigger managed loader and global tracer registration.
    /// Does not actually intercept any methods (unnecessary at this time).
    /// </summary>
    public static class OpenTracingIntegration
    {
        /// <summary>
        /// Noop just used for InterceptMethodAttribute and profiler's assembly loading behavior.
        /// </summary>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        /// <returns>The original method's return value.</returns>
        [InterceptMethod(
            TargetAssembly = "OpenTracing",
            TargetType = "",
            TargetSignatureTypes = new string[0],
            TargetMinimumVersion = "0",
            TargetMaximumVersion = "0")]
        private static object NoopWithoutMatchingMethod(
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            return null;
        }
    }
}
