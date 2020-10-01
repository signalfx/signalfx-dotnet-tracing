# SignalFx Tracing Library for .NET Buildpack

A [CloudFoundry buildpack](https://docs.run.pivotal.io/buildpacks/) to install
and run the SignalFx Tracing Library for .NET in CloudFoundry apps.

## Installation

If you like to install the buildpack, clone this repo and change to this directory, then run:

```sh
$ cf create-buildpack signalfx_dotnet_tracing_buildpack . 99 --enable
```

Now you can use it when running your apps:

```sh
# app configuration
$ cf set-env my-app SIGNALFX_SERVICE_NAME <application name>
$ cf set-env my-app SIGNALFX_ENDPOINT_URL <Smart agent or OTel collector address>
# ...

# dotnet_core_buildpack is the main buildpack for .Net apps, it needs to be the final one
$ cf push my-app -b signalfx_dotnet_tracing_buildpack -b dotnet_core_buildpack
```

## Configuration

You can configure the tracing library using environment variables listed in the [main README.md](../../../README.md).
All configuration options listed there are supported by this buildpack.
