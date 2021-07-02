// Modified by SignalFx
using System;
using System.Net.Http;
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
        const int DefaultTimeoutMilliseconds = 5000;
        private static readonly TimeSpan DefaultTimeoutTimeSpan = TimeSpan.FromMilliseconds(DefaultTimeoutMilliseconds);

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
                CallProduce(topicName, new Message<Null, string> { Value = "test value 1" }, producer.Produce);

                // Kept in the tests temporarily for a crash that is was being caused by wrong signature for Produce methods.
                using var httpClient = new HttpClient();
                await httpClient.GetAsync("https://google.pl");

                CallProduce(topic, new Message<Null, string> { Value = "test value 2" }, producer.Produce);

                await CallProduceAsync(
                    topicName,
                    new Message<Null, string> { Value = "test value 3" },
                    producer.ProduceAsync).ConfigureAwait(false);

                await CallProduceAsync(
                    topicName,
                    new Message<Null, string> { Value = "test value 4" },
                    producer.ProduceAsync).ConfigureAwait(false);

                producer.Flush(DefaultTimeoutTimeSpan);

                await httpClient.GetAsync("https://www.example.com");
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

                // The Kafka calls are all actually happen in the background give it a delay
                // to ensure that the first consume call works.
                Thread.Sleep(DefaultTimeoutMilliseconds);

                try
                {
                    DispalyAndCommitResult(consumer, consumer.Consume(DefaultTimeoutMilliseconds));

                    try
                    {
                       var cts = new CancellationTokenSource(DefaultTimeoutMilliseconds);
                       DispalyAndCommitResult(consumer, consumer.Consume(cts.Token));
                    }
                    catch (OperationCanceledException)
                    {
                       Console.WriteLine("Consume timeout");
                    }

                    DispalyAndCommitResult(consumer, consumer.Consume(DefaultTimeoutTimeSpan));

                    DispalyAndCommitResult(consumer, consumer.Consume(DefaultTimeoutTimeSpan));
                }
                catch (ConsumeException ex)
                {
                    Console.WriteLine($"consume error: {ex.Error.Reason}");
                }
            }
        }

        private static void CallProduce<TTopic>(
            TTopic topic,
            Message<Null, string> message,
            Action<TTopic, Message<Null, string>, Action<DeliveryReport<Null, string>>> produceAction)
        {
            Action<DeliveryReport<Null, string>> deliveryHandler = (DeliveryReport<Null, string> report) =>
            {
                Console.WriteLine($"Produce delivery report received status {report.Status} for message [{report.Message.Value}]");
                if (report.Error != null)
                {
                    Console.WriteLine($"\tError: [{report.Error}]");
                }
            };

            produceAction(topic, message, deliveryHandler);
            Console.WriteLine($"Produce for message [{message.Value}] completed. Wait for error message or delivery report.");
        }

        private static async Task CallProduceAsync<TTopic>(
            TTopic topic,
            Message<Null, string> message,
            Func<TTopic, Message<Null, string>, CancellationToken, Task<DeliveryResult<Null, string>>> produceFuncAsync)
        {
            var cts = new CancellationTokenSource(DefaultTimeoutMilliseconds);
            try
            {
                await produceFuncAsync(topic, message, cts.Token).ConfigureAwait(false);
                Console.WriteLine($"ProduceAsync success for message: [{message.Value}]");
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine($"ProduceAsync timeout for message: [{message.Value}]");
            }
        }

        private static void DispalyAndCommitResult<TKey, TValue>(IConsumer<TKey, TValue> consumer, ConsumeResult<TKey, TValue> result)
        {
            if (result == null)
            {
                Console.WriteLine("Consume result is null");
            }
            else
            {
                consumer.Commit(result);
                Console.WriteLine($"Consume result message: [{result.Message.Value}]");
            }
        }
    }
}
