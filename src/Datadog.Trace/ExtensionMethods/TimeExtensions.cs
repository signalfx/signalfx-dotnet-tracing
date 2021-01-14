// Modified by SignalFx
using System;

namespace SignalFx.Tracing.ExtensionMethods
{
    internal static class TimeExtensions
    {
        /// <summary>
        /// Returns the number of nanoseconds that have elapsed since 1970-01-01T00:00:00.000Z.
        /// </summary>
        /// <param name="dateTimeOffset">The value to get the number of elapsed nanoseconds for.</param>
        /// <returns>The number of nanoseconds that have elapsed since 1970-01-01T00:00:00.000Z.</returns>
        public static long ToUnixTimeNanoseconds(this DateTimeOffset dateTimeOffset)
        {
            return (dateTimeOffset.Ticks - TimeConstants.UnixEpochInTicks) * TimeConstants.NanoSecondsPerTick;
        }

        /// <summary>
        /// Returns the number of microseconds that have elapsed since 1970-01-01T00:00:00.000Z.
        /// </summary>
        /// <param name="dateTimeOffset">The value to get the number of elapsed microseconds for.</param>
        /// <returns>The number of microseconds that have elapsed since 1970-01-01T00:00:00.000Z.</returns>
        public static long ToUnixTimeMicroseconds(this DateTimeOffset dateTimeOffset)
        {
            const long microsecondsPerMillisecond = 1000;
            const long ticksPerMicrosecond = TimeSpan.TicksPerMillisecond / microsecondsPerMillisecond;
            return (dateTimeOffset.Ticks - TimeConstants.UnixEpochInTicks) / ticksPerMicrosecond;
        }

        public static long ToNanoseconds(this TimeSpan ts)
        {
            return ts.Ticks * TimeConstants.NanoSecondsPerTick;
        }

        public static long ToMicroseconds(this TimeSpan ts)
        {
            return ToNanoseconds(ts) / 1000;
        }
    }
}
