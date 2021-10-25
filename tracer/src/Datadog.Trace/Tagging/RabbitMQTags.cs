// <copyright file="RabbitMQTags.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System.Linq;
using Datadog.Trace.ExtensionMethods;

namespace Datadog.Trace.Tagging
{
    internal class RabbitMQTags : MessagingTags
    {
        protected static readonly IProperty<string>[] RabbitMQTagsProperties =
            MessagingTagsProperties.Concat(
                new Property<RabbitMQTags, string>(Tags.InstrumentationName, t => t.InstrumentationName, (t, v) => t.InstrumentationName = v), // Non OTel compliant tag
                new Property<RabbitMQTags, string>(Tags.AmqpCommand, t => t.Command, (t, v) => t.Command = v), // Non OTel compliant tag
                new Property<RabbitMQTags, string>(Tags.AmqpDeliveryMode, t => t.DeliveryMode, (t, v) => t.DeliveryMode = v), // Non OTel compliant tag
                new Property<RabbitMQTags, string>(Tags.RabbitMq.RoutingKey, t => t.RoutingKey, (t, v) => t.RoutingKey = v));

        private string _spanKind;

        // For the sake of unit tests, define a default constructor with the default behavior,
        // though the RabbitMQ integration should use the constructor that takes a spanKind
        // so the setter is only invoked once
        public RabbitMQTags()
        {
            _spanKind = SpanKinds.Client;
        }

        public RabbitMQTags(string spanKind)
        {
            _spanKind = spanKind;
        }

        public string InstrumentationName { get; set; }

        public string Command { get; set; }

        public string DeliveryMode { get; set; }

        public string RoutingKey { get; set; }

        public override string SpanKind => _spanKind;

        protected override IProperty<string>[] GetAdditionalTags() => RabbitMQTagsProperties;
    }
}
