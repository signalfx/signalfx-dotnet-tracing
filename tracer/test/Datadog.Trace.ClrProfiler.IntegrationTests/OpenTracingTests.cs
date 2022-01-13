// Modified by Splunk Inc.

using System.Linq;
using Datadog.Trace.TestHelpers;
using FluentAssertions;
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
            const int expectedSpanCount = 1;

            using var agent = EnvironmentHelper.GetMockAgent();
            using var exit = RunSampleAndWaitForExit(agent.Port, arguments: $"{TestPrefix}", packageVersion: string.Empty);

            var spans = agent.WaitForSpans(expectedSpanCount);

            spans.Count.Should().BeGreaterOrEqualTo(expectedSpanCount);

            var expectedSpan = spans.Where(span => span.Tags.ContainsKey("MyImportantTag") && span.Tags.ContainsKey("FunctionalityReturned")).ToList().FirstOrDefault();
            expectedSpan.Should().NotBeNull();
            expectedSpan.Tags["MyImportantTag"].Should().Be("MyImportantValue");
            expectedSpan.Tags["FunctionalityReturned"].Should().Be("True");
        }
    }
}
