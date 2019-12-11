# SignalFx-Tracing Library for .NET Core

This library provides an OpenTracing-compatible tracer and automatically configurable instrumentations for popular .NET Core libraries and frameworks.  It supports .NET Core 2.0+ on Linux OS distributions (Windows support will be added later).

**All instrumentations are currently in Beta. There are [known .NET Core runtime issues](https://github.com/dotnet/coreclr/issues/18448) for [2.1.0, 2.1.2].**

## Installation

After downloading the [latest release](https://github.com/signalfx/signalfx-dotnet-tracing/releases/latest), you can easily install the CLR Profiler and its components via your system's package manager:

```bash
# Using dpkg:
$ dpkg -i signalfx-dotnet-tracing.deb

# Using rpm:
$ rpm -ivh signalfx-dotnet-tracing.rpm

# Using apk:
# Install libc6-compat dependency and follow release bundle instructions (below).
$ apk add libc6-compat

# Directly from the release bundle:
$ tar -xf signalfx-dotnet-tracing.tar.gz -C /
```

## Usage

The SignalFx-Tracing Library for .NET Core implements the [Profiling API](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/) and should only require basic configuration of your application environment.

```bash
# After installing, configure the required environment variables:
$ source /opt/signalfx-dotnet-tracing/defaults.env

# Then run your application as usual:
$ dotnet run
```

#### About
The SignalFx-Tracing Library for .NET is a fork of the .NET Tracer for Datadog APM that has been modified to provide Zipkin v2 JSON formatting and properly annotated trace data for handling by [SignalFx Microservices APM](https://docs.signalfx.com/en/latest/apm/apm-overview/index.html).
