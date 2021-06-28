namespace Datadog.Trace.ClrProfiler.Integrations.Kafka
{
    internal static class Constants
    {
        internal const string IntegrationName = "Kafka";
        internal const string ProducerType = "Confluent.Kafka.Producer`2";
        internal const string ConsumerType = "Confluent.Kafka.Consumer`2";
        internal const string ConfluentKafkaAssemblyName = "Confluent.Kafka";
        internal const string MinimumVersion = "1.4.0";
        internal const string MaximumVersion = "1.7.0";

        internal const string ConsumeResultTypeName = "Confluent.Kafka.ConsumeResult`2[!0,!1]";
        internal const string TopicPartitionTypeName = "Confluent.Kafka.TopicPartition";
        internal const string MessageTypeName = "Confluent.Kafka.Message`2[!0,!1]";
        internal const string ActionOfDeliveryReportTypeName = "System.Action`1[Confluent.Kafka.DeliveryReport`2[!0,!1]]";
    }
}
