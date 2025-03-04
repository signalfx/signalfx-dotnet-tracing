// <copyright file="SpanContextPropagatorTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using Datadog.Trace.Headers;
using Datadog.Trace.Propagation;
using Datadog.Trace.Propagators;
using FluentAssertions;
using Moq;
using Xunit;

namespace Datadog.Trace.Tests
{
    public class SpanContextPropagatorTests
    {
        private const ulong StringSpanId = 2000000000000000;
        private const ulong SpanId = 2305843009213693952;
        private const int SamplingPriority = 0;

        private static readonly TraceId TraceId = TraceId.CreateRandom();

        private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

        private static readonly KeyValuePair<string, string>[] DefaultHeaderValues =
        {
            new(B3HttpHeaderNames.B3TraceId, TraceId.ToString()),
            new(B3HttpHeaderNames.B3SpanId, StringSpanId.ToString(InvariantCulture)),
            new(B3HttpHeaderNames.B3Sampled, SamplingPriority.ToString(InvariantCulture)),
            new(W3CContextPropagator.TraceParent, FormatW3CTraceParent(TraceId, StringSpanId, SamplingPriority))
        };

        public static TheoryData<string> GetInvalidIds() => new()
        {
            null,
            string.Empty,
            "0",
            "-1",
            "id",
        };

        [Fact]
        public void Inject_IHeadersCollection()
        {
            var context = new SpanContext(TraceId, SpanId, SamplingPriority, serviceName: null);
            var headers = new Mock<IHeadersCollection>();

            SpanContextPropagator.Instance.Inject(context, headers.Object);

            VerifySetCalls(headers);
        }

        [Fact]
        public void Inject_CarrierAndDelegate()
        {
            var context = new SpanContext(TraceId, SpanId, SamplingPriority, serviceName: null);

            // using IHeadersCollection for convenience, but carrier could be any type
            var headers = new Mock<IHeadersCollection>();

            SpanContextPropagator.Instance.Inject(context, headers.Object, (carrier, name, value) => carrier.Set(name, value));

            VerifySetCalls(headers);
        }

        [Fact]
        public void Inject_TraceIdSpanIdOnly()
        {
            var context = new SpanContext(TraceId, SpanId, samplingPriority: null, serviceName: null, origin: null);
            var headers = new Mock<IHeadersCollection>();

            SpanContextPropagator.Instance.Inject(context, headers.Object);

            // null values are not set, so only traceId and spanId (the first two in the list) should be set
            headers.Verify(h => h.Set(B3HttpHeaderNames.B3TraceId, TraceId.ToString()), Times.Once());
            headers.Verify(h => h.Set(B3HttpHeaderNames.B3SpanId, StringSpanId.ToString(InvariantCulture)), Times.Once());
            headers.Verify(h => h.Set(B3HttpHeaderNames.B3Sampled, "0"), Times.Once());
            headers.Verify(h => h.Set(W3CContextPropagator.TraceParent, FormatW3CTraceParent(TraceId, SpanId, 0)), Times.Once());
            headers.VerifyNoOtherCalls();
        }

        [Fact]
        public void Inject_InvalidSampling()
        {
            var context = new SpanContext(TraceId, SpanId, samplingPriority: 1, serviceName: null, origin: null);
            var headers = new Mock<IHeadersCollection>();

            SpanContextPropagator.Instance.Inject(context, headers.Object);

            // null values are not set, so only traceId and spanId (the first two in the list) should be set
            headers.Verify(h => h.Set(B3HttpHeaderNames.B3TraceId, TraceId.ToString()), Times.Once());
            headers.Verify(h => h.Set(B3HttpHeaderNames.B3SpanId, StringSpanId.ToString(InvariantCulture)), Times.Once());
            headers.Verify(h => h.Set(B3HttpHeaderNames.B3Sampled, "1"), Times.Once());
            headers.Verify(h => h.Set(W3CContextPropagator.TraceParent, FormatW3CTraceParent(TraceId, SpanId, 1)), Times.Once());
            headers.VerifyNoOtherCalls();
        }

        [Fact]
        public void Extract_IHeadersCollection()
        {
            var headers = SetupMockHeadersCollection();
            var result = SpanContextPropagator.Instance.Extract(headers.Object);

            VerifyGetCalls(headers);

            result.Should()
                  .BeEquivalentTo(
                       new SpanContextMock
                       {
                           TraceId = TraceId,
                           SpanId = SpanId,
                           SamplingPriority = SamplingPriority,
                       });
        }

        [Fact]
        public void Extract_CarrierAndDelegate()
        {
            // using IHeadersCollection for convenience, but carrier could be any type
            var headers = SetupMockHeadersCollection();
            var result = SpanContextPropagator.Instance.Extract(headers.Object, (carrier, name) => carrier.GetValues(name));

            VerifyGetCalls(headers);

            result.Should()
                  .BeEquivalentTo(
                       new SpanContextMock
                       {
                           TraceId = TraceId,
                           SpanId = SpanId,
                           SamplingPriority = SamplingPriority,
                       });
        }

        [Fact]
        public void Extract_ReadOnlyDictionary()
        {
            var headers = SetupMockReadOnlyDictionary();
            var result = SpanContextPropagator.Instance.Extract(headers.Object);

            VerifyGetCalls(headers);

            result.Should()
                  .BeEquivalentTo(
                       new SpanContextMock
                       {
                           TraceId = TraceId,
                           SpanId = SpanId,
                           SamplingPriority = SamplingPriority,
                       });
        }

        [Fact]
        public void Extract_EmptyHeadersReturnsNull()
        {
            var headers = new Mock<IHeadersCollection>();
            var result = SpanContextPropagator.Instance.Extract(headers.Object);

            result.Should().BeNull();
        }

        [Fact]
        public void Identity()
        {
            var context = new SpanContext(TraceId, SpanId, SamplingPriority, serviceName: null);
            var headers = new NameValueHeadersCollection(new NameValueCollection());

            SpanContextPropagator.Instance.Inject(context, headers);
            var result = SpanContextPropagator.Instance.Extract(headers);

            result.Should().NotBeNull();
            result.Should().NotBeSameAs(context);
            result.TraceId.Should().Be(context.TraceId);
            result.SpanId.Should().Be(context.SpanId);
            result.SamplingPriority.Should().Be(context.SamplingPriority);
        }

        [Theory]
        [MemberData(nameof(GetInvalidIds))]
        public void Extract_InvalidTraceId(string traceId)
        {
            var headers = SetupMockHeadersCollection();

            // replace TraceId setup
            headers.Setup(h => h.GetValues(B3HttpHeaderNames.B3TraceId)).Returns(new[] { traceId });
            headers.Setup(h => h.GetValues(W3CContextPropagator.TraceParent)).Returns(new[] { traceId });

            var result = SpanContextPropagator.Instance.Extract(headers.Object);

            // invalid traceId should return a null context even if other values are set
            result.Should().BeNull();
        }

        [Theory]
        [MemberData(nameof(GetInvalidIds))]
        public void Extract_InvalidSpanId(string spanId)
        {
            var headers = SetupMockHeadersCollection();

            // replace ParentId setup
            headers.Setup(h => h.GetValues(B3HttpHeaderNames.B3SpanId)).Returns(new[] { spanId });
            headers.Setup(h => h.GetValues(W3CContextPropagator.TraceParent)).Returns(new[] { spanId });

            var result = SpanContextPropagator.Instance.Extract(headers.Object);

            result.Should().BeNull();
        }

        [Theory]
        [InlineData("-1000", -1000)]
        [InlineData("1000", 1000)]
        [InlineData("1.0", null)]
        [InlineData("1,0", null)]
        [InlineData(B3HttpHeaderNames.B3Sampled, null)]
        public void Extract_InvalidSamplingPriority(string samplingPriority, int? expectedSamplingPriority)
        {
            // if the extracted sampling priority is a valid integer, pass it along as-is,
            // even if we don't recognize its value to allow forward compatibility with newly added values.
            // ignore the extracted sampling priority if it is not a valid integer.

            var headers = SetupMockHeadersCollection();

            // replace SamplingPriority setup
            headers.Setup(h => h.GetValues(B3HttpHeaderNames.B3Sampled)).Returns(new[] { samplingPriority });

            object result = SpanContextPropagator.Instance.Extract(headers.Object);

            result.Should()
                  .BeEquivalentTo(
                       new SpanContextMock
                       {
                           TraceId = TraceId,
                           SpanId = SpanId,
                           SamplingPriority = expectedSamplingPriority,
                       });
        }

        private static Mock<IHeadersCollection> SetupMockHeadersCollection()
        {
            var headers = new Mock<IHeadersCollection>(MockBehavior.Strict);

            foreach (var pair in DefaultHeaderValues)
            {
                headers.Setup(h => h.GetValues(pair.Key)).Returns(new[] { pair.Value });
            }

            return headers;
        }

        private static Mock<IReadOnlyDictionary<string, string>> SetupMockReadOnlyDictionary()
        {
            var headers = new Mock<IReadOnlyDictionary<string, string>>();

            foreach (var pair in DefaultHeaderValues)
            {
                var value = pair.Value;
                headers.Setup(h => h.TryGetValue(pair.Key, out value)).Returns(true);
            }

            return headers;
        }

        private static void VerifySetCalls(Mock<IHeadersCollection> headers, KeyValuePair<string, string>[] headersToCheck = null)
        {
            var once = Times.Once();

            foreach (var pair in headersToCheck ?? DefaultHeaderValues)
            {
                headers.Verify(h => h.Set(pair.Key, pair.Value), once);
            }

            headers.VerifyNoOtherCalls();
        }

        private static void VerifyGetCalls(Mock<IHeadersCollection> headers)
        {
            var once = Times.Once();

            // B3 is our first propagator, if we are able to fetch data we skip W3C
            foreach (var pair in DefaultHeaderValues.Where(kv => kv.Key != W3CContextPropagator.TraceParent))
            {
                headers.Verify(h => h.GetValues(pair.Key), once);
            }

            headers.VerifyNoOtherCalls();
        }

        private static void VerifyGetCalls(Mock<IReadOnlyDictionary<string, string>> headers)
        {
            var once = Times.Once();
            string value;

            headers.Verify(h => h.TryGetValue(SpanContext.Keys.TraceId, out value), once);

            // B3 is our first propagator, if we are able to fetch data we skip W3C
            foreach (var pair in DefaultHeaderValues.Where(kv => kv.Key != W3CContextPropagator.TraceParent))
            {
                headers.Verify(h => h.TryGetValue(pair.Key, out value), once);
            }

            headers.VerifyNoOtherCalls();
        }

        private static string FormatW3CTraceParent(TraceId traceId, ulong spanId, int samplingPriority)
        {
            return string.Format($"00-{{0}}-{{1}}-{{2}}", TraceId.ToString(), SpanId.ToString("x16"), samplingPriority > 0 ? "01" : "00");
        }
    }

#pragma warning disable SA1402 // File may only contain a single type
    // used to compare property values
    internal class SpanContextMock
    {
        public TraceId TraceId { get; set; }

        public ulong SpanId { get; set; }

        public string Origin { get; set; }

        public int? SamplingPriority { get; set; }

        public ISpanContext Parent { get; set; }

        public ulong? ParentId { get; set; }

        public string ServiceName { get; set; }

        public TraceContext TraceContext { get; set; }
    }
#pragma warning restore SA1402 // File may only contain a single type
}
