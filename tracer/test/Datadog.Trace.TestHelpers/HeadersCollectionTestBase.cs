using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.Headers;
using Xunit;

namespace Datadog.Trace.TestHelpers
{
    public abstract class HeadersCollectionTestBase
    {
        public static IEnumerable<object[]> GetHeaderCollectionImplementations()
        {
            return GetHeaderCollectionFactories().Select(factory => new object[] { factory() });
        }

        public static IEnumerable<object[]> GetHeadersInvalidIdsCartesianProduct()
        {
            return from headersFactory in GetHeaderCollectionFactories()
                   from invalidId in HeadersCollectionTestHelpers.GetInvalidIds().SelectMany(i => i)
                   select new[] { headersFactory(), invalidId };
        }

        public static IEnumerable<object[]> GetHeadersInvalidIntegerSamplingPrioritiesCartesianProduct()
        {
            return from headersFactory in GetHeaderCollectionFactories()
                   from invalidSamplingPriority in HeadersCollectionTestHelpers.GetInvalidIntegerSamplingPriorities().SelectMany(i => i)
                   select new[] { headersFactory(), invalidSamplingPriority };
        }

        public static IEnumerable<object[]> GetHeadersInvalidNonIntegerSamplingPrioritiesCartesianProduct()
        {
            return from headersFactory in GetHeaderCollectionFactories()
                   from invalidSamplingPriority in HeadersCollectionTestHelpers.GetInvalidNonIntegerSamplingPriorities().SelectMany(i => i)
                   select new[] { headersFactory(), invalidSamplingPriority };
        }

        internal static IEnumerable<Func<IHeadersCollection>> GetHeaderCollectionFactories()
        {
            yield return () => WebRequest.CreateHttp("http://localhost").Headers.Wrap();
            yield return () => new NameValueCollection().Wrap();
        }

        internal static void AssertExpected(IHeadersCollection headers, string key, string expected)
        {
            var matches = headers.GetValues(key);
            Assert.Single(matches);
            matches.ToList().ForEach(x => Assert.Equal(expected, x));
        }

        internal static void AssertMissing(IHeadersCollection headers, string key)
        {
            var matches = headers.GetValues(key);
            Assert.Empty(matches);
        }
    }
}
