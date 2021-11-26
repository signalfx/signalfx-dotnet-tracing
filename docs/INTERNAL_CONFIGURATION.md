# Internal Configuration

This section contains list of internal configuration settings (these should not be changed by the users).

## Configuration values

| Environment variable | Description | Default |
|-|-|-|
| `SIGNALFX_EXPORTER` | The exporter to be used. The Tracer uses it to encode and dispatch traces. Available values are: `DatadogAgent`, `Zipkin`. | `Zipkin` |
