# Development

## Windows

Minimum requirements:

- [Visual Studio 2022 (17.0)](https://visualstudio.microsoft.com/downloads/) or newer
  - Workloads
    - Desktop development with C++
    - .NET desktop development
    - .NET Core cross-platform development
    - Optional: ASP.NET and web development (to build samples)
  - Individual components
    - .NET Framework 4.7 targeting pack
- [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
- [.NET 6.0 x86 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) to run 32-bit tests locally
- Optional: [ASP.NET Core 2.1 Runtime](https://dotnet.microsoft.com/download/dotnet-core/2.1) to test in .NET Core 2.1 locally.
- Optional: [ASP.NET Core 3.0 Runtime](https://dotnet.microsoft.com/download/dotnet-core/3.0) to test in .NET Core 3.0 locally.
- Optional: [ASP.NET Core 3.1 Runtime](https://dotnet.microsoft.com/download/dotnet-core/3.1) to test in .NET Core 3.1 locally.
- Optional: [ASP.NET 5.0 Runtime](https://dotnet.microsoft.com/download/dotnet-core/5.0) to test in .NET 5.0 locally.
- Optional: [nuget.exe CLI](https://www.nuget.org/downloads) v5.3 or newer
- Optional: [WiX Toolset 3.11.1](http://wixtoolset.org/releases/) or newer to build Windows installer (msi)
  - [WiX Toolset Visual Studio Extension](https://wixtoolset.org/releases/) to build installer from Visual Studio
- Optional: [Docker for Windows](https://docs.docker.com/docker-for-windows/) to build Linux binaries and run integration tests on Linux containers. See [section on Docker Compose](#building-and-running-tests-with-docker-compose).
- Optional: [Nuke as Global Tool](https://nuke.build/docs/getting-started/setup.html) to be able to call nuke tasks directly.
  - Requires Windows 10 (1607 Anniversary Update, Build 14393 or newer)

This repository uses [Nuke](https://nuke.build/) for build automation.
To see a list of possible targets run:

```cmd
.\tracer\build.cmd --help
```

or, if you installed Nuke as Global Tool

```cmd
nuke --help
```


For example:

```powershell
# Clean and build the main tracer project
.\tracer\build.cmd Clean BuildTracerHome

# Build and run managed and native unit tests. Requires BuildTracerHome to have previously been run
.\tracer\build.cmd BuildAndRunManagedUnitTests BuildAndRunNativeUnitTests 

# Build NuGet packages and MSIs. Requires BuildTracerHome to have previously been run
.\tracer\build.cmd PackageTracerHome 

# Build and run integration tests. Requires BuildTracerHome to have previously been run
.\tracer\build.cmd BuildAndRunWindowsIntegrationTests
```

## Linux

The recommended approach for Linux is to build using Docker. You can use this approach for both Windows and Linux hosts. The _build_in_docker.sh_ script automates building a Docker image with the required dependencies, and running the specified Nuke targets. For example:

```bash
# Clean and build the main tracer project
./tracer/build_in_docker.sh Clean BuildTracerHome

# Build and run managed unit tests. Requires BuildTracerHome to have previously been run
./tracer/build_in_docker.sh BuildAndRunManagedUnitTests 

# Build and run integration tests. Requires BuildTracerHome to have previously been run
./tracer/build_in_docker.sh BuildAndRunLinuxIntegrationTests
```

## Visual Studio Code

This repository contains example configuration for VS Code located under `.vscode.example`. You can copy it to `.vscode`.

```sh
cp -r .vscode.example .vscode
```

### OmniSharp issues

Because of [Mono missing features](https://github.com/OmniSharp/omnisharp-vscode#note-about-using-net-5-sdks), `omnisharp.useGlobalMono` has to be set to `never`. Go to `File` -> `Preferences` -> `Settings` -> `Extensions` -> `C# Configuration` -> Change `Omnisharp: Use Global Mono` (you can search for it if the menu is too long) to `never`. Afterwards, you have restart OmniSharp: `F1` -> `OmniSharp: Restart OmniSharp`.

There may be a lot of errors, because some projects target .NET Framework. Switch to `Datadog.Trace.Minimal.slnf` using `F1` -> `OmniSharp: Select Project` in Visual Studio Code to load a subset of projects which work without any issues. You can also try building the projects which have errors as it sometimes helps.

If for whatever reason you need to use `Datadog.Trace.sln` you can run `./tracer/build.cmd Clean BuildTracerHome` to decrease the number of errors.

## Testing environment

The [`dev/docker-compose.yaml`](../dev/docker-compose.yaml) contains configuration for running OTel Collector and Jaeger.
It also configured to send the traces to Splunk Observability Cloud.

You can run the services using:

```sh
SPLUNK_ACCESS_TOKEN=secret docker-compose -f dev/docker-compose.yaml up
```

The value for `SPLUNK_ACCESS_TOKEN` can be found
[here](https://app.signalfx.com/o11y/#/organization/current?selectedKeyValue=sf_section:accesstokens).

The following Web UI endpoints are exposed:

- <http://localhost:16686/search> - collected traces,
- <http://localhost:8889/metrics> - collected metrics,
- <http://localhost:13133> - collector's health.

## Instrumentation Scripts

> *Caution:* Make sure that before usage you have build the tracer.

[`dev/instrument.sh`](../dev/instrument.sh) helps to run a command with
.NET instrumentation in your shell (e.g. bash, zsh, git bash) .

Example usage:

```sh
./dev/instrument.sh dotnet run -f netcoreapp3.1 -p ./tracer/samples/ConsoleApp/ConsoleApp.csproj --no-launch-profile
```

 [`dev/envvars.sh`](../dev/envvars.sh) can be used to export profiler
 environmental variables to your current shell session.
 **It has to be executed from the root of this repository**.
 Example usage:

 ```sh
 source ./dev/envvars.sh
 ./tracer/samples/ConsoleApp/bin/Debug/netcoreapp3.1/ConsoleApp
 ```

Configuration to send data directly to Splunk Observability Cloud:

 ```sh
export SIGNALFX_ACCESS_TOKEN=secret
export SIGNALFX_ENDPOINT_URL=https://ingest.us0.signalfx.com/v2/trace
```

## Further Reading

- [Profiling API](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/)
- [Metadata API](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/metadata/)
- [The Book of the Runtime - Profiling](https://github.com/dotnet/coreclr/blob/master/Documentation/botr/profiling.md)
