# Development

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

Before running `docker-compose` make sure to set `SPLUNK_AUTH_TOKEN` env var.
You can do this by executing following command in Bash,
where a value for `$SPLUNK_ACCESS_TOKEN` can be found [here](https://app.signalfx.com/o11y/#/organization/current?selectedKeyValue=sf_section:accesstokens).

```sh
export SPLUNK_ACCESS_TOKEN=$(echo -n "auth:$SPLUNK_ACCESS_TOKEN" | base64)
```

You can run the services using:

```sh
docker-compose -f dev/docker-compose.yaml up
```

The following Web UI endpoints are exposed:

- <http://localhost:16686/search> - collected traces,
- <http://localhost:8889/metrics> - collected metrics,
- <http://localhost:13133> - collector's health.

## Instrumentation Scripts

> *Caution:* Make sure that before usage you have build the tracer.

[`dev/instrument.sh`](../dev/instrument.sh) helps to run a command with .NET instrumentation in your shell (e.g. bash, zsh, git bash) .

Example usage:

```sh
./dev/instrument.sh dotnet run -f netcoreapp3.1 -p ./samples/ConsoleApp/ConsoleApp.csproj
```

 [`dev/envvars.sh`](../dev/envvars.sh) can be used to export profiler environmental variables to your current shell session. **It has to be executed from the root of this repository**. Example usage:

 ```sh
 source ./dev/envvars.sh
 ./samples/ConsoleApp/bin/Debug/netcoreapp3.1/ConsoleApp
 ```
