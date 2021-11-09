using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.Headers;

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
            yield return () => new DictionaryHeadersCollection();
        }
    }
}
