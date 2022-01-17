# Azure instrumentation guide

## App Service

1. Choose your app in Azure App Service.
2. Go to **Development Tools > Extensions**.
3. Find and install the **SignalFx .NET Tracing** extension.
4. Go to **Settings > Configuration**.
5. Click **New application setting** to add the following settings:
   * Name: `SIGNALFX_ACCESS_TOKEN`
   * Value: `[splunk-observability-cloud-access-token]`
   * Name: SIGNALFX_ENDPOINT_URL
   * Value: `https://ingest.[splunk-realm].signalfx.com/v2/trace`
6. Restart the application in App Service.

> **Tip:** To reduce latency and benefit from OTel Collector features, you can set the endpoint URL setting to a Collector instance running in Azure VM over an Azure private network.
