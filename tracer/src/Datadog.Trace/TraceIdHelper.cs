// Modified by Splunk Inc.

using System.Runtime.CompilerServices;

namespace Datadog.Trace
{
    internal static class TraceIdHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToString(ulong higher, ulong lower) => $"{higher:x16}{lower:x16}";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToString(long higher, long lower) => ToString((ulong)higher, (ulong)lower);
    }
}
