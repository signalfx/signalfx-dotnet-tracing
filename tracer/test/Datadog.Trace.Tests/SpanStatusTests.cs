// based on: https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/test/OpenTelemetry.Tests/Trace/StatusTest.cs
using Xunit;

namespace Datadog.Trace.Tests
{
    public class SpanStatusTests
    {
        [Fact]
        public void Status_Ok()
        {
            Assert.Equal(StatusCode.Ok, SpanStatus.Ok.StatusCode);
        }

        [Fact]
        public void CheckingDefaultStatus()
        {
            Assert.Equal(default, SpanStatus.Unset);
        }

        [Fact]
        public void Equality()
        {
            var status1 = new SpanStatus(StatusCode.Ok);
            var status2 = new SpanStatus(StatusCode.Ok);
            object status3 = new SpanStatus(StatusCode.Ok);

            Assert.Equal(status1, status2);
            Assert.True(status1 == status2);
            Assert.True(status1.Equals(status3));
        }

        [Fact]
        public void Not_Equality()
        {
            var status1 = new SpanStatus(StatusCode.Ok);
            var status2 = new SpanStatus(StatusCode.Error);
            object notStatus = 1;

            Assert.NotEqual(status1, status2);
            Assert.True(status1 != status2);
            Assert.False(status1.Equals(notStatus));
        }

        [Fact]
        public void TestToString()
        {
            var status = new SpanStatus(StatusCode.Ok);
            Assert.Equal($"SpanStatus{{StatusCode={status.StatusCode}}}", status.ToString());
        }

        [Fact]
        public void TestGetHashCode()
        {
            var status = new SpanStatus(StatusCode.Ok);
            Assert.NotEqual(0, status.GetHashCode());
        }
    }
}
