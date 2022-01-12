# Azure instrumentation guide

## App Service

1. Choose your App Service.
2. Navigate to `Development Tools` > `Extensions`.
3. Find and install the `SignalFx .NET Tracing` extension.
4. Navigate to `Settings` > `Configuration`.
5. Add `New application setting`s to configure the receiver:

    ```
    Name: SIGNALFX_ACCESS_TOKEN 
    Value: (Your SIGNALFX access token)

    Name: SIGNALFX_ENDPOINT_URL
    Value: (Your Collector or SignalFX ingest endpoint)
    ```
    (See [advanced-config.md](advanced-config.md) for more options.)
6. Restart the App Service.