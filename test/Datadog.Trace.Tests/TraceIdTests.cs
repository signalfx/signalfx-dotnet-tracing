using System;
using FluentAssertions;
using FluentAssertions.Execution;
using SignalFx.Tracing;
using Xunit;

namespace Datadog.Trace.Tests
{
    public class TraceIdTests
    {
        [Fact]
        public void CreateRandom_CreatesValid128BitId()
        {
            var traceId = TraceId.CreateRandom();

            using (new AssertionScope())
            {
                traceId.ToString().Should().HaveLength(32);
                FluentActions.Invoking(() => Convert.ToUInt64(traceId.ToString().Substring(startIndex: 0, length: 16), fromBase: 16)).Should().NotThrow();
                FluentActions.Invoking(() => Convert.ToUInt64(traceId.ToString().Substring(startIndex: 16, length: 16), fromBase: 16)).Should().NotThrow();
            }
        }

        [Fact]
        public void Parse_CreatesIdCorrectly()
        {
            var traceId = TraceId.CreateRandom();
            var recreatedId = TraceId.Parse(traceId.ToString());

            recreatedId.Should().Be(traceId);
        }

        [Fact]
        public void TryParse_CreatesIdCorrectly()
        {
            var traceId = TraceId.CreateRandom();
            var result = TraceId.TryParse(traceId.ToString(), out var recreatedId);

            using (new AssertionScope())
            {
                result.Should().BeTrue();
                recreatedId.Should().Be(traceId);
            }
        }

        [Fact]
        public void TryParse_ForInvalidTraceId_ReturnsFalse()
        {
            var traceId = "321521";
            var result = TraceId.TryParse(traceId, out var recreatedId);

            using (new AssertionScope())
            {
                result.Should().BeFalse();
                recreatedId.Should().Be(TraceId.Zero);
            }
        }

        [Fact]
        public void CreateFromInt_CreatesIdCorrectly()
        {
            var traceId = TraceId.CreateFromInt(123);

            traceId.ToString().Should().Be("0000000000000000000000000000007b");
        }

        [Fact]
        public void CreateFromUlong_CreatesIdCorrectly()
        {
            var traceId = TraceId.CreateFromUlong(3212132132132132121);

            traceId.ToString().Should().Be("00000000000000002c93c927d35a9519");
        }

        [Fact]
        public void Lower_Returns64LowerBitsOfId()
        {
            var traceId = TraceId.CreateRandom();

            traceId.Lower.Should().Be(Convert.ToUInt64(traceId.ToString().Substring(startIndex: 16, length: 16), fromBase: 16));
        }

        [Fact]
        public void Equals_WorksCorrectly()
        {
            var traceId1 = TraceId.CreateRandom();

            traceId1.Should().Be(TraceId.Parse(traceId1.ToString()));
        }

        [Fact]
        public void Zero_ReturnsEmptyId()
        {
            var traceId = TraceId.Zero;

            using (new AssertionScope())
            {
                traceId.ToString().Should().HaveLength(32);
                traceId.ToString().Should().Be("00000000000000000000000000000000");
            }
        }
    }
}
