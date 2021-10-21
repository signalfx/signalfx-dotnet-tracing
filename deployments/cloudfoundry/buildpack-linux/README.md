# SignalFx Tracing Library for .NET Buildpack (Linux)

A [CloudFoundry buildpack](https://docs.run.pivotal.io/buildpacks/) to install
and run the SignalFx Tracing Library for .NET in CloudFoundry apps on the Linux stack.

> :construction: This project is currently in **BETA**.

## Installation

To build and install the buildpack without using the tile you need to have
[cfcli](https://docs.cloudfoundry.org/cf-cli/install-go-cli.html) installed.

If you would like to install the buildpack, clone this repo, change to this directory, then run:

```sh
$ ./build.sh

# installs the buildpack on CloudFoundry
$ cf create-buildpack signalfx_dotnet_tracing_buildpack signalfx_dotnet_tracing_buildpack-linux.zip 99 --enable
```

Now you can use the buildpack when running your apps:

```sh
# app configuration
$ cf set-env my-app SIGNALFX_SERVICE_NAME <application name>
$ cf set-env my-app SIGNALFX_ENDPOINT_URL <Smart agent or OTel collector address>
# ...

# dotnet_core_buildpack is the main buildpack for .NET apps, it needs to be the final one
$ cf push my-app -b signalfx_dotnet_tracing_buildpack -b dotnet_core_buildpack
```

## Configuration

You can configure the tracing library using environment variables listed in the [main README.md](../../../README.md).
All configuration options listed there are supported by this buildpack.

By default, the tracing library logs are stored in the `/home/vcap/logs/signalfx-dotnet-profiler.log` file, but you can change this behavior with `SIGNALFX_TRACE_LOG_PATH` environment variable.

If you want to use a specific version of the tracing library in your application, you can set the `SIGNALFX_DOTNET_TRACING_VERSION`
environment variable before application deployment, either using `cf set-env` or the `manifest.yml` file:

```sh
$ cf set-env SIGNALFX_DOTNET_TRACING_VERSION "0.1.14"
```
