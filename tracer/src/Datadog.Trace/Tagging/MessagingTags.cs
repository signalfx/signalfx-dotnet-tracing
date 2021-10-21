// Modified by Splunk Inc.

using System.Linq;
using Datadog.Trace.ExtensionMethods;

namespace Datadog.Trace.Tagging
{
    internal abstract class MessagingTags : InstrumentationTags
    {
        protected static readonly IProperty<string>[] MessagingTagsProperties =
            InstrumentationTagsProperties.Concat(
                new Property<MessagingTags, string>(Tags.Messaging.System, t => t.System, (t, v) => t.System = v),
                new Property<MessagingTags, string>(Tags.Messaging.Destination, t => t.Destination, (t, v) => t.Destination = v),
                new Property<MessagingTags, string>(Tags.Messaging.DestinationKind, t => t.DestinationKind, (t, v) => t.DestinationKind = v),
                new Property<MessagingTags, string>(Tags.Messaging.TempDestination, t => t.TempDestination, (t, v) => t.TempDestination = v),
                new Property<MessagingTags, string>(Tags.Messaging.Protocol, t => t.Protocol, (t, v) => t.Protocol = v),
                new Property<MessagingTags, string>(Tags.Messaging.ProtocolVersion, t => t.ProtocolVersion, (t, v) => t.ProtocolVersion = v),
                new Property<MessagingTags, string>(Tags.Messaging.Url, t => t.Url, (t, v) => t.Url = v),
                new Property<MessagingTags, string>(Tags.Messaging.Operation, t => t.Operation, (t, v) => t.Operation = v),
                new Property<MessagingTags, string>(Tags.Messaging.MessageId, t => t.MessageId, (t, v) => t.MessageId = v),
                new Property<MessagingTags, string>(Tags.Messaging.ConversationId, t => t.ConversationId, (t, v) => t.ConversationId = v),
                new Property<MessagingTags, string>(Tags.Messaging.ConsumerId, t => t.ConsumerId, (t, v) => t.ConsumerId = v),
                new Property<MessagingTags, string>(Tags.Messaging.MessagePayloadSize, t => t.MessagePayloadSize, (t, v) => t.MessagePayloadSize = v),
                new Property<MessagingTags, string>(Tags.Messaging.MessagePayloadCompressedSize, t => t.MessagePayloadCompressedSize, (t, v) => t.MessagePayloadCompressedSize = v));

        public string System { get; set; }

        public string Destination { get; set; }

        public string DestinationKind { get; set; }

        public string TempDestination { get; set; }

        public string Protocol { get; set; }

        public string ProtocolVersion { get; set; }

        public string Url { get; set; }

        public string Operation { get; set; }

        public string MessageId { get; set; }

        public string ConversationId { get; set; }

        public string ConsumerId { get; set; }

        public string MessagePayloadSize { get; set; }

        public string MessagePayloadCompressedSize { get; set; }

        protected override IProperty<string>[] GetAdditionalTags() => MessagingTagsProperties;
    }
}
