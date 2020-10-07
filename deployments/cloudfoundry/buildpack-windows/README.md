# SignalFx Tracing Library for .NET Buildpack (Windows)

A [CloudFoundry buildpack](https://docs.run.pivotal.io/buildpacks/) to install
and run the SignalFx Tracing Library for .NET in CloudFoundry apps on the Windows stack.

> :construction: This project is currently in **BETA**.

## Installation

To build and install the buildpack you need to have [Go](https://golang.org/dl/) and [cfcli](https://docs.cloudfoundry.org/cf-cli/install-go-cli.html) installed.

If you would like to install the buildpack, clone this repo, change to this directory, then run:

```sh
# builds supply.exe executable
$ ./build.sh

# installs the buildpack on CloudFoundry
$ cf create-buildpack signalfx_dotnet_tracing_buildpack . 99 --enable
```

to build and install the buildpack.
Now you can use the buildpack when running your apps (both .NET Core and .NET Framework apps are supported):

```sh
# app configuration
$ cf set-env my-app SIGNALFX_SERVICE_NAME <application name>
$ cf set-env my-app SIGNALFX_ENDPOINT_URL <Smart agent or OTel collector address>
# ...

# for .NET Core apps:
# binary_buildpack should be used for .NET Core Windows apps, it needs to be the final one
$ cf push my-app -b signalfx_dotnet_tracing_buildpack -b binary_buildpack -s windows

# for .NET Framework apps:
# you need to enable HWC explicitly when using this buildpack
$ cf set-env my-app SIGNALFX_USE_HWC true
# hwc_buildpack should be used for .NET Framework Windows apps, it needs to be the final one
$ cf push my-app -b signalfx_dotnet_tracing_buildpack -b hwc_buildpack -s windows
```

## Configuration

You can configure the tracing library using environment variables listed in the [main README.md](../../../README.md).
All configuration options listed there are supported by this buildpack.

By default, the tracing library logs are stored in the `c:\Users\vcap\logs\signalfx-dotnet-profiler.log` file, but you can change this behavior with `SIGNALFX_TRACE_LOG_PATH` environment variable.

If you want to use a specific version of the tracing library in your application, you can set the `SIGNALFX_DOTNET_TRACING_VERSION`
environment variable before application deployment, either using `cf set-env` or the `manifest.yml` file:

```sh
$ cf set-env SIGNALFX_DOTNET_TRACING_VERSION "0.1.2"
```

If you want to use this buildpack in a .NET Framework application deployment you have to enable HWC support by setting the `SIGNALFX_USE_HWC` environment variable to `true` either using `cf set-env` or the `manifest.yml` file:

```sh
$ cf set-env my-app SIGNALFX_USE_HWC true
```

Any other value means that HWC is not used.
