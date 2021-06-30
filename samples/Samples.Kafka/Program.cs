// Modified by SignalFx
using System;
using Confluent.Kafka;

namespace Samples.Kafka
{
    // Run following command to prepare the Kafka cluster:
    //   docker-compose up kafka
    // Based mostly on:
    // - https://github.com/confluentinc/confluent-kafka-dotnet/blob/v1.7.0/examples/ConfluentCloud/Program.cs
    public static class Program
    {
        public static void Main(string[] args)
        {
            // If calls of instrumented methods are directly on Main the instrumentation doesn't happen:
            // [warn] JITCompilationStarted skipping method: Method replacement found but the managed profiler has not yet been loaded into AppDomain
            ProduceConsumeUsingKafka();
        }
        
        private static void ProduceConsumeUsingKafka()
        {
            var kafkaUrl = Environment.GetEnvironmentVariable("KAFKA_HOST") ?? "localhost:29092";
            var topicName = "dotnet-test-topic";
            var topic = new TopicPartition(topicName, Partition.Any);

            var pConfig = new ProducerConfig { BootstrapServers = kafkaUrl };
            using (var producer = new ProducerBuilder<Null, string>(pConfig).Build())
            {
                producer.Produce(topic, new Message<Null, string> { Value = "test value" });
                producer.Flush(TimeSpan.FromSeconds(value: 10));
            }

            var cConfig = new ConsumerConfig
            {
                BootstrapServers = kafkaUrl,
                GroupId = Guid.NewGuid().ToString(),
                AutoOffsetReset = AutoOffsetReset.Earliest
            };
            using (var consumer = new ConsumerBuilder<Null, string>(cConfig).Build())
            {
                consumer.Subscribe(topicName);
                try
                {
                    var consumeResult = consumer.Consume(10000);
                    Console.WriteLine($"consumed: {consumeResult?.Message?.Value}");
                }
                catch (ConsumeException ex)
                {
                    Console.WriteLine($"consume error: {ex.Error.Reason}");
                }
            }
        }
    }
}
