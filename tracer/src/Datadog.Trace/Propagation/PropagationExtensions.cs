using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.Headers;

namespace Datadog.Trace.Propagation
{
    internal static class PropagationExtensions
    {
        internal const string HttpRequestHeadersTagPrefix = "http.request.headers";
        internal const string HttpResponseHeadersTagPrefix = "http.response.headers";

        private static readonly ConcurrentDictionary<Key, string> DefaultTagMappingCache = new ConcurrentDictionary<Key, string>();

        public static void Inject(this IPropagator propagator, SpanContext context, IHeadersCollection headers)
        {
            propagator.Inject(context, headers, InjectToHeadersCollection);
        }

        public static SpanContext Extract(this IPropagator propagator, IHeadersCollection headers)
        {
            return propagator.Extract(headers, ExtractFromHeadersCollection);
        }

        public static IEnumerable<KeyValuePair<string, string>> ExtractHeaderTags(this IHeadersCollection headers, IEnumerable<KeyValuePair<string, string>> headerToTagMap, string defaultTagPrefix)
        {
            return ExtractHeaderTags(headers, headerToTagMap, defaultTagPrefix, string.Empty);
        }

        public static IEnumerable<KeyValuePair<string, string>> ExtractHeaderTags(this IHeadersCollection headers, IEnumerable<KeyValuePair<string, string>> headerToTagMap, string defaultTagPrefix, string userAgent)
        {
            foreach (KeyValuePair<string, string> headerNameToTagName in headerToTagMap)
            {
                var headerName = headerNameToTagName.Key;
                var providedTagName = headerNameToTagName.Value;

                string headerValue;
                if (string.Equals(headerName, CommonHttpHeaderNames.UserAgent, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(userAgent))
                {
                    // A specific case for the user agent as it is splitted in .net framework web api.
                    headerValue = userAgent;
                }
                else
                {
                    headerValue = ParseString(headers, headerName);
                }

                if (headerValue is null)
                {
                    continue;
                }

                // Tag name is normalized during Tracer instantiation so use as-is
                if (!string.IsNullOrWhiteSpace(providedTagName))
                {
                    yield return new KeyValuePair<string, string>(providedTagName, headerValue);
                }
                else
                {
                    // Since the header name was saved to do the lookup in the input headers,
                    // convert the header to its final tag name once per prefix
                    var cacheKey = new Key(headerName, defaultTagPrefix);
                    string tagNameResult = DefaultTagMappingCache.GetOrAdd(cacheKey, key =>
                    {
                        if (key.HeaderName.TryConvertToNormalizedTagName(normalizePeriods: true, out var normalizedHeaderTagName))
                        {
                            return key.TagPrefix + "." + normalizedHeaderTagName;
                        }
                        else
                        {
                            return null;
                        }
                    });

                    if (tagNameResult != null)
                    {
                        yield return new KeyValuePair<string, string>(tagNameResult, headerValue);
                    }
                }
            }
        }

        public static string ParseString(this IHeadersCollection headers, string headerName)
        {
            return PropagationHelpers.ParseString(headers, (carrier, header) => carrier.GetValues(header), headerName);
        }

        private static void InjectToHeadersCollection(IHeadersCollection carrier, string header, string value)
        {
            carrier.Set(header, value);
        }

        private static IEnumerable<string> ExtractFromHeadersCollection(IHeadersCollection carrier, string header)
        {
            return carrier.GetValues(header);
        }

        private struct Key : IEquatable<Key>
        {
            public readonly string HeaderName;
            public readonly string TagPrefix;

            public Key(
                string headerName,
                string tagPrefix)
            {
                HeaderName = headerName;
                TagPrefix = tagPrefix;
            }

            /// <summary>
            /// Gets the struct hashcode
            /// </summary>
            /// <returns>Hashcode</returns>
            public override int GetHashCode()
            {
                unchecked
                {
                    return (HeaderName.GetHashCode() * 397) ^ TagPrefix.GetHashCode();
                }
            }

            /// <summary>
            /// Gets if the struct is equal to other object or struct
            /// </summary>
            /// <param name="obj">Object to compare</param>
            /// <returns>True if both are equals; otherwise, false.</returns>
            public override bool Equals(object obj)
            {
                return obj is Key key &&
                       HeaderName == key.HeaderName &&
                       TagPrefix == key.TagPrefix;
            }

            /// <inheritdoc />
            public bool Equals(Key other)
            {
                return HeaderName == other.HeaderName &&
                       TagPrefix == other.TagPrefix;
            }
        }
    }
}
