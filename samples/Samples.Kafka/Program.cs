// Modified by SignalFx
using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using SignalFx.Tracing;

namespace Samples.Kafka
{
    // Run following command to prepare the Kafka cluster:
    //   docker-compose up kafka
    // Based mostly on:
    // - https://github.com/confluentinc/confluent-kafka-dotnet/blob/v1.7.0/examples/ConfluentCloud/Program.cs
    public static class Program
    {
        private const int DefaultTimeoutMilliseconds = 5000;
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

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = kafkaUrl,
            };

            var producerBuilder = new ProducerBuilder<Null, string>(producerConfig)
                .SetErrorHandler((_, err) =>
                {
                    Console.WriteLine($"Producer error handler: {err}");
                })
                .SetLogHandler((_, logMessage) =>
                {
                    Console.WriteLine($"Producer log handler: {logMessage.Message}");
                });

            using (var producer = producerBuilder.Build())
            {
                CallProduce(topicName, new Message<Null, string> { Value = "test value 1" }, producer);

                CallProduce(topic, new Message<Null, string> { Value = "test value 2" }, producer);

                await CallProduceAsync(
                    topicName,
                    new Message<Null, string> { Value = "test value 3" },
                    producer).ConfigureAwait(false);

                await CallProduceAsync(
                    topic,
                    new Message<Null, string> { Value = "test value 4" },
                    producer).ConfigureAwait(false);

                producer.Flush(DefaultTimeoutTimeSpan);
            }

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = kafkaUrl,
                GroupId = "b412aa02-509b-40dc-9452-ad872d75f3f2",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            var consumerBuilder = new ConsumerBuilder<Null, string>(consumerConfig)
                .SetErrorHandler((_, err) =>
                {
                    Console.WriteLine($"Consumer error handler: {err}");
                })
                .SetLogHandler((_, logMessage) =>
                {
                    Console.WriteLine($"Consumer log handler: {logMessage.Message}");
                });

            using (var consumer = consumerBuilder.Build())
            {
                consumer.Subscribe(topicName);

                // The Kafka calls are all actually happen in the background give it a delay
                // to ensure that the first consume call works.
                Thread.Sleep(DefaultTimeoutMilliseconds);

                try
                {
                    DisplayAndCommitResult(consumer, consumer.Consume(DefaultTimeoutMilliseconds));

                    try
                    {
                        var cts = new CancellationTokenSource(DefaultTimeoutMilliseconds);
                        DisplayAndCommitResult(consumer, consumer.Consume(cts.Token));
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine("Consume timeout");
                    }

                    DisplayAndCommitResult(consumer, consumer.Consume(DefaultTimeoutTimeSpan));

                    DisplayAndCommitResult(consumer, consumer.Consume(DefaultTimeoutTimeSpan));
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
            IProducer<Null, string> producer)
        {
            Action<DeliveryReport<Null, string>> deliveryHandler = (DeliveryReport<Null, string> report) =>
            {
                Console.WriteLine($"Produce delivery report received status {report.Status} for message [{report.Message.Value}]");
                if (report.Error != null)
                {
                    Console.WriteLine($"\tError: [{report.Error}]");
                }
            };

            using var scope = Tracer.Instance.StartActive($"{nameof(CallProduce)}({topic.GetType()})");

            try
            {
                if (topic is string topicName)
                {
                    producer.Produce(topicName, message, deliveryHandler);
                }
                else if (topic is TopicPartition topicPartition)
                {
                    producer.Produce(topicPartition, message, deliveryHandler);
                }
                else
                {
                    throw new ArgumentException("Unkonwn topic type: " + topic.GetType());
                }
            }
            catch (Exception ex)
            {
                scope.Span.SetException(ex);
                throw;
            }

            Console.WriteLine($"Produce for message [{message.Value}] completed. Wait for error message or delivery report.");
        }

        private static async Task CallProduceAsync<TTopic>(
            TTopic topic,
            Message<Null, string> message,
            IProducer<Null, string> producer)
        {
            using var scope = Tracer.Instance.StartActive($"{nameof(CallProduceAsync)}({topic.GetType()})");
            try
            {
                var cts = new CancellationTokenSource(DefaultTimeoutMilliseconds);
                DeliveryResult<Null, string> deliveryResult = null;
                if (topic is string topicName)
                {
                    Console.WriteLine("ProduceAsync with string");
                    deliveryResult = await producer.ProduceAsync(topicName, message, cts.Token);
                }
                else if (topic is TopicPartition topicPartition)
                {
                    Console.WriteLine("ProduceAsync with TopicPartition");
                    deliveryResult = await producer.ProduceAsync(topicPartition, message, cts.Token);
                }
                else
                {
                    throw new ArgumentException("Unkonwn topic type: " + topic.GetType());
                }

                Console.WriteLine($"ProduceAsync success for message: [{message.Value}]");
                Console.WriteLine($"\tDeliveryResult: [{deliveryResult.Status}]");
            }
            catch (TaskCanceledException tcException)
            {
                Console.WriteLine($"ProduceAsync timeout for message: [{message.Value}]");
                scope.Span.SetException(tcException);
            }
            catch (Exception ex)
            {
                scope.Span.SetException(ex);
                throw;
            }
        }

        private static void DisplayAndCommitResult<TKey, TValue>(IConsumer<TKey, TValue> consumer, ConsumeResult<TKey, TValue> result)
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

        private static void DeliveryHandler(DeliveryReport<Null, string> report)
        {
            Console.WriteLine($"Produce delivery report received status {report.Status} for message [{report.Message.Value}]");
            if (report.Error != null && report.Error.IsError)
            {
                Console.WriteLine($"\tError: [{report.Error}]");
            }
        }
    }
}
