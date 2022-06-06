// Modified by Splunk Inc.

using Datadog.Trace.AlwaysOnProfiler.Builder;
using FluentAssertions;
using Xunit;

namespace Datadog.Trace.Tests.AlwaysOnProfiler.Builder
{
    public class LabelBuilderTests
    {
        private readonly LabelBuilder _labelBuilder = new();

        [Fact]
        public void SetKey()
        {
            _labelBuilder.SetKey(100);

            var label = _labelBuilder.Build();

            label.Key.Should().Be(100);
        }

        [Fact]
        public void SetNum()
        {
            _labelBuilder.SetNum(200);

            var label = _labelBuilder.Build();

            label.Num.Should().Be(200);
        }

        [Fact]
        public void SetStr()
        {
            _labelBuilder.SetStr(1);

            var label = _labelBuilder.Build();

            label.Str.Should().Be(1);
        }
    }
}
