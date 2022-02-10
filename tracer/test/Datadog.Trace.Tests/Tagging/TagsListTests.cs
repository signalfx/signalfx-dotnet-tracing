// <copyright file="TagsListTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Datadog.Trace.Agent.MessagePack;
using Datadog.Trace.ClrProfiler;
using Datadog.Trace.SourceGenerators;
using Datadog.Trace.Tagging;
using Datadog.Trace.TestHelpers;
using Datadog.Trace.Util;
using Datadog.Trace.Vendors.MessagePack;
using FluentAssertions;
using Moq;
using Xunit;

namespace Datadog.Trace.Tests.Tagging
{
    public class TagsListTests
    {
        [Fact]
        public void SetTag_WillNotCauseDuplicates()
        {
            // Initialize common tags
            var tags = new CommonTags()
            {
                Version = "v1.0",
                Environment = "Test"
            };

            // Initialize custom tags
            tags.SetTag("sample.1", "Temp 1");
            tags.SetTag("sample.2", "Temp 2");

            // Try set existing tag
            tags.SetTag(Tags.Version, "v2.0");
            tags.SetTag("sample.2", "Temp 3");

            var all = tags.GetAllTags();
            var distinctKeys = all.Select(x => x.Key).Distinct().Count();

            Assert.Equal(all.Count, distinctKeys);
            Assert.Single(all, x => x.Key == Tags.Version && x.Value == "v2.0");
            Assert.Single(all, x => x.Key == "sample.2" && x.Value == "Temp 3");
        }

        [Fact]
        public void GetAll()
        {
            // Should be any actual implementation
            var tags = new CommonTags();
            var values = new[]
            {
                "v1.0", "Test", "value 1", "value 2"
            };

            tags.Version = values[0];
            tags.Environment = values[1];

            tags.SetTag("sample.1", values[2]);
            tags.SetTag("sample.2", values[3]);

            ValidateTags(tags.GetAllTags(), values);
        }

        [Fact]
        public void GetAll_When_MissingTags()
        {
            var tags = new EmptyTags();
            var values = Array.Empty<string>();

            ValidateTags(tags.GetAllTags(), values);
        }

        [Fact]
        public void CheckProperties()
        {
            var assemblies = new[] { typeof(TagsList).Assembly, typeof(SqlTags).Assembly };

            foreach (var type in assemblies.SelectMany(a => a.GetTypes()))
            {
                if (!typeof(TagsList).IsAssignableFrom(type))
                {
                    continue;
                }

                if (type.IsInterface || type.IsAbstract)
                {
                    continue;
                }

                var random = new Random();

                Action<ITags, string, string> setTag = (tagsList, name, value) => tagsList.SetTag(name, value);
                Func<ITags, string, string> getTag = (tagsList, name) => tagsList.GetTag(name);
                Action<ITags, string, double?> setMetric = (tagsList, name, value) => tagsList.SetMetric(name, value);
                Func<ITags, string, double?> getMetric = (tagsList, name) => tagsList.GetMetric(name);

                ValidateProperties(type, setTag, getTag, () => Guid.NewGuid().ToString());
                ValidateProperties(type, setMetric, getMetric, () => random.NextDouble());
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Serialization(bool topLevelSpan)
        {
            var tags = new CommonTags();

            Span span;

            if (topLevelSpan)
            {
                span = new Span(new SpanContext(TraceId.CreateFromInt(42), 41), DateTimeOffset.UtcNow, tags);
            }
            else
            {
                // Assign a parent to prevent the span from being considered as top-level
                var traceContext = new TraceContext(Mock.Of<IDatadogTracer>());
                var parent = new SpanContext(TraceId.CreateFromInt(42), 41);
                span = new Span(new SpanContext(parent, traceContext, null), DateTimeOffset.UtcNow, tags);
            }

            // The span has 1 "common" tag and 15 additional tags (and same number of metrics)
            // Those numbers are picked to test the variable-size header of MessagePack
            // The header is resized when there are 16 or more elements in the collection
            // Neither common or additional tags have enough elements, but put together they will cause to use a bigger header
            tags.Environment = "Test";
            tags.SamplingLimitDecision = 0.5;

            for (int i = 0; i < 15; i++)
            {
                span.SetTag(i.ToString(), i.ToString());
            }

            for (int i = 0; i < 15; i++)
            {
                span.SetMetric(i.ToString(), i);
            }

            var buffer = new byte[0];

            // use vendored MessagePack to serialize
            var resolver = new FormatterResolverWrapper(SpanFormatterResolver.Instance);
            Vendors.MessagePack.MessagePackSerializer.Serialize(ref buffer, 0, span, resolver);

            // use nuget MessagePack to deserialize
            var deserializedSpan = global::MessagePack.MessagePackSerializer.Deserialize<MockSpan>(buffer);

            // For top-level spans, there is one tag added during serialization
            Assert.Equal(topLevelSpan ? 17 : 16, deserializedSpan.Tags.Count);

            // For top-level spans, there is one metric added during serialization
            Assert.Equal(topLevelSpan ? 17 : 16, deserializedSpan.Metrics.Count);

            Assert.Equal("Test", deserializedSpan.Tags[Tags.Env]);
            Assert.Equal(0.5, deserializedSpan.Metrics[Metrics.SamplingLimitDecision]);

            for (int i = 0; i < 15; i++)
            {
                Assert.Equal(i.ToString(), deserializedSpan.Tags[i.ToString()]);
                Assert.Equal((double)i, deserializedSpan.Metrics[i.ToString()]);
            }

            if (topLevelSpan)
            {
                Assert.Equal(Tracer.RuntimeId, deserializedSpan.Tags[Tags.RuntimeId]);
                Assert.Equal(1.0, deserializedSpan.Metrics[Metrics.TopLevelSpan]);
            }
        }

        private static void ValidateProperties<T>(Type type, Action<ITags, string, T> setTagValue, Func<ITags, string, T> getTagValue, Func<T> valueGenerator)
        {
            var instance = (ITags)Activator.CreateInstance(type);
            var isTag = typeof(T) == typeof(string);

            var allProperties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                    .Where(p => p.PropertyType == typeof(T));

            var propertyAndTagName = allProperties
                                    .Select(property =>
                                     {
                                         var name = isTag
                                                        ? property.GetCustomAttribute<TagAttribute>()?.TagName
                                                        : property.GetCustomAttribute<MetricAttribute>()?.MetricName;
                                         return (property, tagOrMetric: name);
                                     })
                                    .ToArray();

            propertyAndTagName
               .Should()
               .OnlyContain(x => !string.IsNullOrEmpty(x.tagOrMetric));

            var writeableProperties = propertyAndTagName.Where(p => p.property.CanWrite).ToArray();
            var readonlyProperties = propertyAndTagName.Where(p => !p.property.CanWrite).ToArray();

            // ---------- Test read-write properties
            var testValues = Enumerable.Range(0, writeableProperties.Length).Select(_ => valueGenerator()).ToArray();

            for (var i = 0; i < writeableProperties.Length; i++)
            {
                var (property, tagName) = writeableProperties[i];
                var testValue = testValues[i];

                setTagValue(instance, tagName, testValue);

                property.GetValue(instance).Should().Be(testValue, $"Getter and setter mismatch for tag {property.Name} of type {type.Name}");

                var actualValue = getTagValue(instance, tagName);

                actualValue.Should().Be(testValue, $"Getter and setter mismatch for tag {property.Name} of type {type.Name}");
            }

            // Check that all read/write properties were mapped
            var remainingValues = new HashSet<T>(testValues);

            foreach (var property in writeableProperties)
            {
                remainingValues.Remove((T)property.property.GetValue(instance))
                               .Should()
                               .BeTrue($"Property {property.property.Name} of type {type.Name} is not mapped");
            }

            // ---------- Test readonly properties
            remainingValues = new HashSet<T>(readonlyProperties.Select(p => (T)p.property.GetValue(instance)));

            foreach (var propertyAndTag in readonlyProperties)
            {
                var tagName = propertyAndTag.tagOrMetric;
                var tagValue = getTagValue(instance, tagName);

                remainingValues.Remove(tagValue)
                               .Should()
                               .BeTrue($"Property {propertyAndTag.property.Name} of type {type.Name} is not mapped");
            }
        }

        private void ValidateTags(List<KeyValuePair<string, string>> tags, string[] values)
        {
            Assert.True(tags.Count >= values.Length); // At least specified values

            if (values.Length > 0)
            {
                Assert.Contains(values, v => values.Contains(v));
            }
        }

        internal class EmptyTags : TagsList
        {
        }
    }
}
