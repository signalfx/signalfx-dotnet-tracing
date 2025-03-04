# allow overriding defaults using environment variables
if (Test-Path env:SvcFabDir) { $SvcFabDir = $env:SvcFabDir } else { $SvcFabDir = 'D:\SvcFab' }
if (Test-Path env:SIGNALFX_TRACER_VERSION) { $SIGNALFX_TRACER_VERSION = $env:SIGNALFX_TRACER_VERSION } else { $SIGNALFX_TRACER_VERSION = '0.0.1' }
if (Test-Path env:SIGNALFX_TRACER_URL) { $SIGNALFX_TRACER_URL = $env:SIGNALFX_TRACER_URL } else { $SIGNALFX_TRACER_URL = "https://github.com/DataDog/dd-trace-dotnet/releases/download/v$SIGNALFX_TRACER_VERSION/windows-tracer-home.zip" }
if (Test-Path env:SIGNALFX_DOTNET_TRACER_HOME) { $SIGNALFX_DOTNET_TRACER_HOME = $env:SIGNALFX_DOTNET_TRACER_HOME } else { $SIGNALFX_DOTNET_TRACER_HOME = "$SvcFabDir\datadog-dotnet-tracer\v$SIGNALFX_TRACER_VERSION" }

Write-Host "[DatadogInstall.ps1] Installing Datadog .NET Tracer v$SIGNALFX_TRACER_VERSION"

# download, extract, and delete the archive
$ArchivePath = "$SvcFabDir\windows-tracer-home.zip"
Write-Host "[DatadogInstall.ps1] Downloading $SIGNALFX_TRACER_URL to $ArchivePath"
Invoke-WebRequest $SIGNALFX_TRACER_URL -OutFile $ArchivePath

Write-Host "[DatadogInstall.ps1] Extracting to $SIGNALFX_DOTNET_TRACER_HOME"
Expand-Archive -Force -Path "$SvcFabDir\windows-tracer-home.zip" -DestinationPath $SIGNALFX_DOTNET_TRACER_HOME

Write-Host "[DatadogInstall.ps1] Deleting $ArchivePath"
Remove-Item $ArchivePath

# create a folder for log files
$LOGS_PATH = "$SvcFabDir\datadog-dotnet-tracer-logs"

if (-not (Test-Path -Path $LOGS_PATH -PathType Container)) {
  Write-Host "[DatadogInstall.ps1] Creating logs folder $LOGS_PATH"
  New-Item -ItemType Directory -Force -Path $LOGS_PATH
}

function Set-MachineEnvironmentVariable {
    param(
      [string]$name,
      [string]$value
    )

    Write-Host "[DatadogInstall.ps1] Setting environment variable $name=$value"
    [System.Environment]::SetEnvironmentVariable($name, $value, [System.EnvironmentVariableTarget]::Machine)
}

Set-MachineEnvironmentVariable 'SIGNALFX_DOTNET_TRACER_HOME' $SIGNALFX_DOTNET_TRACER_HOME
Set-MachineEnvironmentVariable 'SIGNALFX_TRACE_LOG_DIRECTORY' "$LOGS_PATH"

# Set-MachineEnvironmentVariable'COR_ENABLE_PROFILING' '0' # Enable per app
Set-MachineEnvironmentVariable 'COR_PROFILER' '{B4C89B0F-9908-4F73-9F59-0D77C5A06874}'
Set-MachineEnvironmentVariable 'COR_PROFILER_PATH_32' "$SIGNALFX_DOTNET_TRACER_HOME\win-x86\SignalFx.Tracing.ClrProfiler.Native.dll"
Set-MachineEnvironmentVariable 'COR_PROFILER_PATH_64' "$SIGNALFX_DOTNET_TRACER_HOME\win-x64\SignalFx.Tracing.ClrProfiler.Native.dll"

# Set-MachineEnvironmentVariable 'CORECLR_ENABLE_PROFILING' '0' # Enable per app
Set-MachineEnvironmentVariable 'CORECLR_PROFILER' '{B4C89B0F-9908-4F73-9F59-0D77C5A06874}'
Set-MachineEnvironmentVariable 'CORECLR_PROFILER_PATH_32' "$SIGNALFX_DOTNET_TRACER_HOME\win-x86\SignalFx.Tracing.ClrProfiler.Native.dll"
Set-MachineEnvironmentVariable 'CORECLR_PROFILER_PATH_64' "$SIGNALFX_DOTNET_TRACER_HOME\win-x64\SignalFx.Tracing.ClrProfiler.Native.dll"
