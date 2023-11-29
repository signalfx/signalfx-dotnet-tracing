# OpenTelemetry Profiles Scouting

The [`otel-profiles`](https://github.com/signalfx/signalfx-dotnet-tracing/tree/otel-profiles)
branch is used to experiment with [OpenTelemetry Proto Profile](https://github.com/open-telemetry/opentelemetry-proto-profile)
also known as `PProfExtended`.

## Developing instructions

Use the [PProfExtendedPrototype.sln](../../PProfExtendedPrototype.sln) to open
the key projects dealing with profiling code.

Targeted building instructions:

Windows:

```Powershell
dir -r .\tracer\src\*.csproj | % { dotnet restore $_ }

.\tracer\build.ps1 BuildTracerHome -Skip Restore

dotnet test -c Release -f net6.0 --filter "AlwaysOnProfiler" .\tracer\test\Datadog.Trace.Tests\Datadog.Trace.Tests.csproj

.\tracer\build.ps1 PublishAlwaysOnProfilerNativeDepWindows

dotnet build -c Release -f net6.0 -p:Platform=x64 .\tracer\test\test-applications\integrations\Samples.AlwaysOnProfiler\Samples.AlwaysOnProfiler.csproj

dotnet build -c Release -f net6.0 .\tracer\test\test-applications\debugger\Samples.Probes\Samples.Probes.csproj

dotnet test -c Release -f net6.0 --filter "AlwaysOnProfiler" .\tracer\test\Datadog.Trace.ClrProfiler.IntegrationTests\Datadog.Trace.ClrProfiler.IntegrationTests.csproj
```

Linux:

```bash
find ./tracer/src/ -name "*.csproj" | xargs -n 1 dotnet restore

./tracer/build.sh BuildTracerHome -Skip Restore

dotnet test -c Release -f net6.0 --filter "AlwaysOnProfiler" ./tracer/test/Datadog.Trace.Tests/Datadog.Trace.Tests.csproj

./tracer/build.sh PublishAlwaysOnProfilerNativeDepLinux

dotnet publish -c Release -f net6.0 ./tracer/test/test-applications/integrations/Samples.AlwaysOnProfiler/Samples.AlwaysOnProfiler.csproj

dotnet build -c Release -f net6.0 ./tracer/test/test-applications/debugger/Samples.Probes/Samples.Probes.csproj

dotnet test -c Release -f net6.0 --filter "AlwaysOnProfiler" ./tracer/test/Datadog.Trace.ClrProfiler.IntegrationTests/Datadog.Trace.ClrProfiler.IntegrationTests.csproj
```
