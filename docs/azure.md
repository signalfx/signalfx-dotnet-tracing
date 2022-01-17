# Azure instrumentation guide

## App Service

1. Choose your app in Azure App Service.
2. Go to `Development Tools` > `Extensions`.
3. Find and install the `SignalFx .NET Tracing` extension.
4. Go to `Settings` > `Configuration`.
5. Add the following `New application setting` to configure the receiver:

    ```
    Name: SIGNALFX_ACCESS_TOKEN 
    Value: <splunk-observability-cloud-access-token>

    Name: SIGNALFX_ENDPOINT_URL
    Value: https://ingest.<splunk-realm>.signalfx.com/v2/trace
    ```
    (See [advanced-config.md](advanced-config.md) for more options.)
6. Restart the App Service.
