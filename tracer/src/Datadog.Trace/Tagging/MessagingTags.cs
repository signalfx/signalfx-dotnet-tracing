// Modified by Splunk Inc.

using Datadog.Trace.SourceGenerators;

namespace Datadog.Trace.Tagging
{
    internal abstract partial class MessagingTags : InstrumentationTags
    {
        [Tag(Trace.Tags.Messaging.System)]
        public string System { get; set; }

        [Tag(Trace.Tags.Messaging.Destination)]
        public string Destination { get; set; }

        [Tag(Trace.Tags.Messaging.DestinationKind)]
        public string DestinationKind { get; set; }

        [Tag(Trace.Tags.Messaging.TempDestination)]
        public string TempDestination { get; set; }

        [Tag(Trace.Tags.Messaging.Protocol)]
        public string Protocol { get; set; }

        [Tag(Trace.Tags.Messaging.ProtocolVersion)]
        public string ProtocolVersion { get; set; }

        [Tag(Trace.Tags.Messaging.Url)]
        public string Url { get; set; }

        [Tag(Trace.Tags.Messaging.Operation)]
        public string Operation { get; set; }

        [Tag(Trace.Tags.Messaging.MessageId)]
        public string MessageId { get; set; }

        [Tag(Trace.Tags.Messaging.ConversationId)]
        public string ConversationId { get; set; }

        [Tag(Trace.Tags.Messaging.ConsumerId)]
        public string ConsumerId { get; set; }

        [Tag(Trace.Tags.Messaging.MessageSize)]
        public string MessageSize { get; set; }

        [Tag(Trace.Tags.Messaging.MessageSizeCompressed)]
        public string MessageSizeCompressed { get; set; }
    }
}
