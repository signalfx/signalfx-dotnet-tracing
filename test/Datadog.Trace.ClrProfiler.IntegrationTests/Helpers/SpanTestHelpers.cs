// Modified by SignalFx
using System;
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.TestHelpers;
using Xunit;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public class SpanTestHelpers
    {
        public static void AssertExpectationsMet<T>(
            List<T> expectations,
            List<IMockSpan> spans)
            where T : SpanExpectation
        {
            Assert.True(spans.Count >= expectations.Count, $"Expected at least {expectations.Count} spans, received {spans.Count}");

            var failures = new List<string>();
            var remainingSpans = spans.Select(s => s).ToList();

            var spanDump = new List<string>();
            spanDump.Add($"{Environment.NewLine} >>>>>>> Received spans:");
            foreach (var span in spans)
            {
                spanDump.Add(span.ToString());
            }

            foreach (var expectation in expectations)
            {
                var possibleSpans =
                    remainingSpans
                       .Where(s => expectation.Matches(s))
                       .ToList();

                if (possibleSpans.Count == 0)
                {
                    failures.Add($"No spans for: {expectation}");
                    continue;
                }

                var resultSpan = possibleSpans.First();

                if (!remainingSpans.Remove(resultSpan))
                {
                    throw new Exception("Failed to remove an inspected span, can't trust this test.'");
                }

                if (!expectation.MeetsExpectations(resultSpan, out var failureMessage))
                {
                    failures.Add($"{expectation} failed with: {failureMessage}");
                }
            }

            var spansMsg = Environment.NewLine + string.Join(Environment.NewLine, spanDump.Select(s => " * " + s));

            var finalMessage = Environment.NewLine + string.Join(Environment.NewLine, failures.Select(f => " - " + f));

            Assert.True(!failures.Any(), finalMessage + spansMsg);
            Assert.True(remainingSpans.Count == 0, $"There were {remainingSpans.Count} spans unaccounted for.");
        }
    }
}
