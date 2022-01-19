# Azure instrumentation guide

## App Service

1. Choose your app in Azure App Service.

2. Go to **Development Tools > Extensions**.

3. Find and install the **SignalFx .NET Tracing** extension.

4. Go to **Settings > Configuration**.

5. Click **New application setting** to add the following settings:

   Name of the instrumented service:

   * Name: `SIGNALFX_SERVICE_NAME`
   * Value: `my-service-name`

   Deployment environment of the instrumented service:

   * Name: `SIGNALFX_ENV`
   * Value: `development`

   Access token: See [here](https://docs.splunk.com/Observability/admin/authentication-tokens/org-tokens.html)
   to learn how to obtain one.

   * Name: `SIGNALFX_ACCESS_TOKEN`
   * Value: `[secret]`

   In the endpoint URL, ``splunk-realm`` is the [O11y realm](https://dev.splunk.com/observability/docs/realms_in_endpoints).
   For example, ``us0``.

   * Name: `SIGNALFX_ENDPOINT_URL`
   * Value: `https://ingest.[splunk-realm].signalfx.com/v2/trace`

6. Restart the application in App Service.

> **Tip:** To reduce latency and benefit from OTel Collector features,
> you can set the endpoint URL setting to a Collector instance running
> in Azure VM over an Azure private network.
