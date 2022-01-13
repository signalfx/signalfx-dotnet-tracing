// Modified by Splunk Inc.

using System.Linq;
using Datadog.Trace.TestHelpers;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public class OpenTracingTests : TestHelper
    {
        public OpenTracingTests(ITestOutputHelper output)
            : base("OpenTracing", output)
        {
            SetServiceVersion("1.0.0");
        }

        [SkippableFact]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        public void SubmitTraces()
        {
            const int expectedSpanCount = 2;

            using var agent = EnvironmentHelper.GetMockAgent();
            using var exit = RunSampleAndWaitForExit(agent.Port, arguments: $"{TestPrefix}", packageVersion: string.Empty);

            var spans = agent.WaitForSpans(expectedSpanCount);

            spans.Count.Should().BeGreaterOrEqualTo(expectedSpanCount);

            var expectedOuterSpan = spans.Where(span => !span.ParentId.HasValue).ToList().FirstOrDefault();
            var expectedInnerSpan = spans.Where(span => span.ParentId.HasValue).ToList().FirstOrDefault();

            using var scope = new AssertionScope();
            expectedOuterSpan.Should().NotBeNull();
            expectedInnerSpan.Should().NotBeNull();
            expectedOuterSpan.ParentId.HasValue.Should().BeFalse();
            expectedInnerSpan.ParentId.Value.Should().Be(expectedOuterSpan.SpanId);
            expectedOuterSpan.Tags["MyImportantTag"].Should().Be("MyImportantValue");
            expectedOuterSpan.Tags["FunctionalityReturned"].Should().Be("True");
            expectedInnerSpan.Tags["InnerSpanTag"].Should().Be("ImportantValue");
        }
    }
}
