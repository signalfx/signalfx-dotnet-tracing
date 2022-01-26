$Env:COR_ENABLE_PROFILING="1"
$Env:COR_PROFILER="{B4C89B0F-9908-4F73-9F59-0D77C5A06874}"
$Env:COR_PROFILER_PATH=(Join-Path $PWD "\..\..\..\bin\tracer-home\win-x64\SignalFx.Tracing.ClrProfiler.Native.dll" | Resolve-Path)

$Env:SIGNALFX_DOTNET_TRACER_HOME=(Join-Path $PWD "\..\..\..\bin\tracer-home\" | Resolve-Path)

$Env:SIGNALFX_ENV="dev"
$Env:SIGNALFX_SERVICE_NAME="NLog40Example"
$Env:SIGNALFX_VERSION="1.0.0"
$Env:SIGNALFX_LOGS_INJECTION="true"

dotnet run