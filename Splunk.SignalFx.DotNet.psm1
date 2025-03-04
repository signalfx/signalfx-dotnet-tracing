#
# NB: This install module is expected by signalfx/splunk-otel-collector
#

#Requires -RunAsAdministrator

function Get-TempPath() {
    $temp = $env:TEMP

    if(-not (Test-Path $temp)) {
        New-Item -ItemType Directory -Force -Path $temp | Out-Null
    }

    return $temp
}

function Reset-IIS() {    
    Start-Process "iisreset.exe" -NoNewWindow -Wait
}

<#
    .SYNOPSIS
    Installs SignalFx Instrumentation for .NET.
#>
function Install-SignalFxDotnet() {
    # signalfx-dotnet-tracing github repository API
    $release = "1.1.1"
    $api = "https://api.github.com/repos/signalfx/signalfx-dotnet-tracing/releases/tags/v$($release)"

    # determine OS architecture
    $os_bits = (Get-CimInstance Win32_OperatingSystem).OSArchitecture
    $os_arch = (&{If($os_bits -eq "64-bit") {"x64"} Else {"x86"}})

    # File pattern to search for
    $asset_name = "signalfx-dotnet-tracing-$release-$os_arch.msi"
    $msi = $null

    try {
        # Find MSI to download
        $download = (Invoke-WebRequest $api -UseBasicParsing | ConvertFrom-Json).assets | Where-Object { $_.name -eq $asset_name } | Select-Object -Property browser_download_url,name

        # Download installer MSI to Temp
        $msi = Get-TempPath | Join-Path -ChildPath $download.name
        Invoke-WebRequest -Uri $download.browser_download_url -OutFile $msi -UseBasicParsing
    }
    catch {
        $msg = $_
        throw "Could not download $($asset_name): $msg"
    }

    # Install downloaded MSI
    $process = Start-Process msiexec.exe -Wait -PassThru -ArgumentList "/I $msi /quiet"

    if ($process.ExitCode -ne 0 -and $process.ExitCode -ne 3010) {
        throw "Could not install. The installer returned error code $($process.ExitCode).`r`n" + 
        "See more: https://learn.microsoft.com/en-us/windows/win32/msi/error-codes"
    }

    Reset-IIS

    Remove-Item $msi
}

<#
    .SYNOPSIS
    Gets if SignalFx Instrumentation for .NET is installed.
#>
function Get-IsSignalFxInstalled() {
    $path = [System.Environment]::GetEnvironmentVariable("SIGNALFX_DOTNET_TRACER_HOME", [System.EnvironmentVariableTarget]::Machine)

    if ($path) {
        return Test-Path $path
    }

    return $false
}

Export-ModuleMember -Function Install-SignalFxDotnet
Export-ModuleMember -Function Get-IsSignalFxInstalled