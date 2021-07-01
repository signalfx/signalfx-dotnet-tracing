namespace Datadog.Trace.ClrProfiler.Integrations.Kafka
{
    internal static class Constants
    {
        internal const string IntegrationName = "Kafka";
        internal const string ConsumeSyncOperationName = "kafka.consume";
        internal const string ProduceSyncMethodName = "Produce";
        internal const string ProduceAsyncMethodName = "ProduceAsync";
        internal const string ConsumeSyncMethodName = "Consume";
        internal const string ConsumeAsyncMethodName = "ConsumeAsync";
        internal const string ProducerType = "Confluent.Kafka.IProducer`2";
        internal const string ConsumerType = "Confluent.Kafka.IConsumer`2";
        internal const string HeadersType = "Confluent.Kafka.Headers";
        internal const string ConfluentKafkaAssemblyName = "Confluent.Kafka";
        internal const string MinimumVersion = "1.4.0";
        internal const string MaximumVersion = "1";

        internal const string ConsumeResultTypeName = "Confluent.Kafka.ConsumeResult`2<T, T>";
        internal const string TopicPartitionTypeName = "Confluent.Kafka.TopicPartition";
        internal const string MessageTypeName = "Confluent.Kafka.Message`2<T, T>";
        internal const string ActionOfDeliveryReportTypeName = "System.Action`1<Confluent.Kafka.DeliveryReport`2<T, T>>";
    }
}
