# SignalFx-Tracing Library for .NET Core on Linux

This library provides an OpenTracing-compatible tracer and automatically configured instrumentations for popular .NET Core libraries and frameworks.  It supports .NET Core 2.0+ on Linux OS distributions (Windows support will be added later).

## Supported Libraries and Frameworks

**All instrumentations are currently in Beta. There are [known .NET Core runtime issues](https://github.com/dotnet/coreclr/issues/18448) for [2.1.0, 2.1.2].**

| Library | Versions Supported | Notes |
| ---     | ---                | ---   |
| ADO.NET | Supported .NET Core versions | |
| ASP.NET Core MVC | 2.0+ | `Microsoft.AspNet.Mvc.Core` NuGet and built-in packages |
| Elasticsearch.Net | `Elasticsearch.Net` Nuget 5.3 - 6.x | Disable `db.statement` tagging with `SIGNALFX_INSTRUMENTATION_ELASTICSEARCH_TAG_QUERIES=false` (`true` by default, which may introduce overhead for direct streaming users). |
| HttpClient | Supported .NET Core versions | by way of `System.Net.Http.HttpClientHandler` and `HttpMessageHandler` instrumentations |
| MongoDB | `MongoDB.Driver.Core` Nuget 2.1.0+ | Disable `db.statement` tagging with `SIGNALFX_INSTRUMENTATION_MONGODB_TAG_COMMANDS=false` (`true` by default). |
| WebClient | Supported .NET Core versions | by way of `System.Net.WebRequest` instrumentation |

## Installation

After downloading the [latest release](https://github.com/signalfx/signalfx-dotnet-tracing/releases/latest), you can easily install the CLR Profiler and its components via your system's package manager:

```bash
# Using dpkg:
$ dpkg -i signalfx-dotnet-tracing.deb

# Using rpm:
$ rpm -ivh signalfx-dotnet-tracing.rpm

# Directly from the release bundle:
$ tar -xf signalfx-dotnet-tracing.tar.gz -C /
```

## Usage

The SignalFx-Tracing Library for .NET Core implements the [Profiling API](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/) and should only require basic configuration of your application environment.

```bash
# After installing, configure the required environment variables:
$ source /opt/signalfx-dotnet-tracing/defaults.env

# Optionally create the default logging directory:
$ mkdir /var/log/signalfx

# Configure you desired service name:
$ export SIGNALFX_SERVICE_NAME='MyCoreService'

# Configure reporting to your Smart Agent or Gateway instance:
# (http://localhost:9080/v1/trace by default)
$ export SIGNALFX_ENDPOINT_URL='http://<MyAgentOrGateway>:9080/v1/trace'

# Then run your application as usual:
$ dotnet run
```

## Custom Instrumentation

In cases where you desire more customized performance-monitoring capabilities, you can build upon the provided tracing functionality by modifying and adding to automatically generated traces.
The SignalFx Tracing library for .NET Core provides and registers an [OpenTracing-compatible](https://github.com/opentracing/opentracing-csharp) global tracer you can use to this end:

```xml
<!-- Add the OpenTracing dependency to your project -->
<PackageReference Include="OpenTracing" Version="0.12.1" />
```

```csharp
using OpenTracing;
using OpenTracing.Util;

namespace MyProject
{
    public class MyClass
    {
        public static async void MyMethod()
        {
            // Obtain the automatically registered OpenTracing.Util.GlobalTracer instance
            var tracer = GlobalTracer.Instance;

            // Create an active span that will be automatically parented by any existing span in this context
            using (IScope scope = tracer.BuildSpan("MyTracedFunctionality").StartActive(finishSpanOnDispose: true))
            {
                var span = scope.Span;
                span.SetTag("MyImportantTag", "MyImportantValue");
                span.Log("My Important Log Statement");

                var ret = await MyAppFunctionality();

                span.SetTag("FunctionalityReturned", ret.ToString());
            }
        }
    }
}
```

In this way, the tracing insights provided by auto-instrumentation can act as the basis of any introspective elements you add.  By using both instrumentation approaches, you'll be able to present a more detailed representation of the logic and functionality of your application, clients, and framework.  OpenTracing versions 0.12.0+ are supported and the provided tracer offers a complete implementation of the OpenTracing API.

#### About
The SignalFx-Tracing Library for .NET Core on Linux is a fork of the .NET Tracer for Datadog APM that has been modified to provide Zipkin v2 JSON formatting, a complete OpenTracing API implementation, B3 propagation, and properly annotated trace data for handling by [SignalFx Microservices APM](https://docs.signalfx.com/en/latest/apm/apm-overview/index.html).
