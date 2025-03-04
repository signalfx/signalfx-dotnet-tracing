// <copyright file="TimeExtensions.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;

namespace Datadog.Trace.ExtensionMethods
{
    internal static class TimeExtensions
    {
        private const long MicrosecondsPerMillisecond = 1000;
        private const long TicksPerMicrosecond = TimeSpan.TicksPerMillisecond / MicrosecondsPerMillisecond;

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
            return (dateTimeOffset.Ticks - TimeConstants.UnixEpochInTicks) / TicksPerMicrosecond;
        }

        public static long ToNanoseconds(this TimeSpan ts)
        {
            return ts.Ticks * TimeConstants.NanoSecondsPerTick;
        }

        public static long ToMicroseconds(this TimeSpan ts)
        {
            return ts.Ticks / TicksPerMicrosecond;
        }
    }
}
