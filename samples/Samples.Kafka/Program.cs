// Modified by SignalFx
using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace Samples.Kafka
{
    // Run following command to prepare the Kafka cluster:
    //   docker-compose up kafka
    // Based mostly on:
    // - https://github.com/confluentinc/confluent-kafka-dotnet/blob/v1.7.0/examples/ConfluentCloud/Program.cs
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            // If calls of instrumented methods are directly on Main the instrumentation doesn't happen:
            // [warn] JITCompilationStarted skipping method: Method replacement found but the managed profiler has not yet been loaded into AppDomain
            await ProduceConsumeUsingKafka();
        }
        
        private static async Task ProduceConsumeUsingKafka()
        {
            var kafkaUrl = Environment.GetEnvironmentVariable("KAFKA_HOST") ?? "localhost:29092";
            var topicName = "dotnet-test-topic";
            var topic = new TopicPartition(topicName, Partition.Any);

            var pConfig = new ProducerConfig { BootstrapServers = kafkaUrl };
            using (var producer = new ProducerBuilder<Null, string>(pConfig).Build())
            {
                producer.Produce(topicName, new Message<Null, string> { Value = "test value" });
                producer.Produce(topic, new Message<Null, string> { Value = "test value 2" });
                await producer.ProduceAsync(topicName, new Message<Null, string> { Value = "test value 3" });
                await producer.ProduceAsync(topic, new Message<Null, string> { Value = "test value 4" });
                producer.Flush(TimeSpan.FromSeconds(value: 10));
            }

            var cConfig = new ConsumerConfig
            {
                BootstrapServers = kafkaUrl,
                GroupId = "b412aa02-509b-40dc-9452-ad872d75f3f2",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            using (var consumer = new ConsumerBuilder<Null, string>(cConfig).Build())
            {
                consumer.Subscribe(topicName);
                try
                {
                    DispalyAndCommitResult(consumer, consumer.Consume(10000));

                    try
                    {
                       var cts = new CancellationTokenSource(5000);
                       DispalyAndCommitResult(consumer, consumer.Consume(cts.Token));
                    }
                    catch (OperationCanceledException)
                    {
                       // Expected, just ignore it.
                       Console.WriteLine("Consume timedout");
                    }

                    DispalyAndCommitResult(consumer, consumer.Consume(TimeSpan.FromSeconds(10)));

                    DispalyAndCommitResult(consumer, consumer.Consume(TimeSpan.FromSeconds(10)));
                }
                catch (ConsumeException ex)
                {
                    Console.WriteLine($"consume error: {ex.Error.Reason}");
                }
            }
        }

        private static void DispalyAndCommitResult<TKey, TValue>(IConsumer<TKey, TValue> consumer, ConsumeResult<TKey, TValue> result)
        {
            if (result == null)
            {
                Console.WriteLine("result is null");
            }
            else
            {
                consumer.Commit(result);
                Console.WriteLine($"consumed: {result.Message.Value}");
            }
        }
    }
}
