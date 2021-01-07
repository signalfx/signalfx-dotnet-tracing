// Modified by SignalFx
#if !NET45 && !NET451 && !NET452
using System;
using SignalFx.Tracing.ExtensionMethods;
using Xunit;

namespace Datadog.Trace.Tests
{
    public class TimeUtilsTests
    {
        [Fact]
        public void ToUnixTimeNanoseconds_UnixEpoch_Zero()
        {
            var date = DateTimeOffset.FromUnixTimeMilliseconds(0);
            Assert.Equal(0, date.ToUnixTimeNanoseconds());
        }

        [Fact]
        public void ToUnixTimeNanoseconds_Now_CorrectMillisecondRoundedValue()
        {
            var date = DateTimeOffset.UtcNow;
            Assert.Equal(date.ToUnixTimeMilliseconds(), date.ToUnixTimeNanoseconds() / 1000000);
        }

        [Fact]
        public void ToUnixTimeMicroseconds_UnixEpoch_Zero()
        {
            var date = DateTimeOffset.FromUnixTimeMilliseconds(0);
            Assert.Equal(0, date.ToUnixTimeMicroseconds());
        }

        [Fact]
        public void ToUnixTimeMicrooseconds_Now_CorrectMillisecondRoundedValue()
        {
            var date = DateTimeOffset.UtcNow;
            Assert.Equal(date.ToUnixTimeMilliseconds(), date.ToUnixTimeMicroseconds() / 1000);
        }
    }
}
#endif