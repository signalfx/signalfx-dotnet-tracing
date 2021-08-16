# SignalFx Tracing Library for .NET Release Notes

## [Unreleased]

- Use OpenTelemetry semantic conventions for log correlation
- Remove spans for Confluent.Kafka Consume calls that didn't receive a message
- Add configuration setting, `SIGNALFX_OUTBOUND_HTTP_EXCLUDED_HOSTS`, that prevents the creation of outbound HTTP spans for certain hosts
- Added Server-Timing header to ASP.NET on IIS

[Commits](https://github.com/signalfx/signalfx-dotnet-tracing/v0.1.12...HEAD)

[Full diff](https://github.com/signalfx/signalfx-dotnet-tracing/v0.1.12..HEAD)

## [Release 0.1.12](https://github.com/signalfx/signalfx-dotnet-tracing/releases/tag/v0.1.12)

- Support for .NET 5.0
- Added Confluent.Kafka instrumentation for IConsumer.Consume, IProducer.Produce, and IProducer.ProduceAsync
- New instrumentations for SqlCommand: ExecuteXmlReader and ExecuteXmlReaderAsync methods

[Commits](https://github.com/signalfx/signalfx-dotnet-tracing/v0.1.11...v0.1.12)

[Full diff](https://github.com/signalfx/signalfx-dotnet-tracing/v0.1.11..v0.1.12)

## [Release 0.1.11](https://github.com/signalfx/signalfx-dotnet-tracing/releases/tag/v0.1.11)

- Optimized log injection for NLog 4.6+
- Optimized B3 context propagation
- Other optimizations: default sampler and span Id allocation

[Commits](https://github.com/signalfx/signalfx-dotnet-tracing/v0.1.10...v0.1.11)

[Full diff](https://github.com/signalfx/signalfx-dotnet-tracing/v0.1.10..v0.1.11)

---

## Previous Releases

See [releases page](https://github.com/signalfx/signalfx-dotnet-tracing/releases).