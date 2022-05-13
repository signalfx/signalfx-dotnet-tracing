using System.Collections.Generic;
using Datadog.Trace.Agent;
using Datadog.Trace.ClrProfiler.AutoInstrumentation.GraphQL;
using Datadog.Trace.Configuration;
using Datadog.Trace.DogStatsd;
using FluentAssertions;
using Moq;
using Xunit;

namespace Datadog.Trace.ClrProfiler.Managed.Tests.AutoInstrumentation.GraphQL
{
    public class GraphQLCommonTests
    {
        [Fact]
        public void Mapped_service_name_should_be_used_if_provided_for_validate()
        {
            var settings = DefaultSettings();

            settings.SetServiceNameMappings(new[] { new KeyValuePair<string, string>("graphql", "my-custom-name") });

            var span = CreateSpanFromValidate(settings);

            span.ServiceName.Should().Be("my-custom-name", "service name mappings should be reflected in service name");
            span.Tags.GetTag(Tags.Version).Should().BeNull("service version should not be set for spans with mapped service name");
        }

        [Fact]
        public void Default_service_name_and_version_should_be_used_if_no_mapping_is_configured_for_validate()
        {
            var settings = DefaultSettings();

            var span = CreateSpanFromValidate(settings);

            span.ServiceName.Should().Be("TestService", "default service name should be used");
            span.Tags.GetTag(Tags.Version).Should().Be("1.2.3", "service version should be set for spans with default service name");
        }

        [Fact]
        public void Mapped_service_name_should_be_used_if_provided_for_execute()
        {
            var settings = DefaultSettings();

            settings.SetServiceNameMappings(new[] { new KeyValuePair<string, string>("graphql", "my-custom-name") });

            var span = CreateSpanFromExecute(settings);

            span.ServiceName.Should().Be("my-custom-name", "service name mappings should be reflected in service name");
            span.Tags.GetTag(Tags.Version).Should().BeNull("service version should not be set for spans with mapped service name");
        }

        [Fact]
        public void Default_service_name_and_version_should_be_used_if_no_mapping_is_configured_for_execute()
        {
            var settings = DefaultSettings();

            var span = CreateSpanFromExecute(settings);

            span.ServiceName.Should().Be("TestService", "default service name should be used");
            span.Tags.GetTag(Tags.Version).Should().Be("1.2.3", "service version should be set for spans with default service name");
        }

        private static Span CreateSpanFromExecute(TracerSettings settings)
        {
            var tracer = new Tracer(settings, new Mock<IAgentWriter>().Object, null, null, new NoOpStatsd());

            var document = Mock.Of<IDocument>(doc => doc.OriginalQuery == "query");
            var executionContext =
                Mock.Of<IExecutionContext>(
                    ec =>
                        ec.Document == document &&
                        ec.Operation == Mock.Of<IOperation>(op => op.Name == "query" && op.OperationType == OperationTypeProxy.Query));
            var scope = GraphQLCommon.CreateScopeFromExecuteAsync(tracer, executionContext);

            var span = scope.Span;
            return span;
        }

        private static Span CreateSpanFromValidate(TracerSettings settings)
        {
            var tracer = new Tracer(settings, new Mock<IAgentWriter>().Object, null, null, new NoOpStatsd());

            var scope = GraphQLCommon.CreateScopeFromValidate(tracer, "query");

            var span = scope.Span;
            return span;
        }

        private static TracerSettings DefaultSettings()
        {
            return new TracerSettings { ServiceName = "TestService", ServiceVersion = "1.2.3" };
        }
    }
}
