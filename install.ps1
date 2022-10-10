#
# NB: This install script is expected by signalfx/splunk-otel-collector (install.ps1)
#

# signalfx-dotnet-tracing github repository API
$release = "v0.2.9"
$api = "https://api.github.com/repos/signalfx/signalfx-dotnet-tracing/releases/tags/$release"

# determine OS architecture
$os_bits = (Get-CimInstance Win32_OperatingSystem).OSArchitecture
$os_arch = (&{If($os_bits -eq "64-bit") {"x64"} Else {"x86"}})

# File pattern to search for
$pattern = "signalfx-dotnet-tracing-*-$os_arch.msi"

# Find latest MSI to download
$download = (Invoke-WebRequest $api | ConvertFrom-Json).assets | Where-Object { $_.name -like $pattern } | Select-Object -Property browser_download_url,name

# Download installer MSI to Temp
$msi = Join-Path $env:temp $download.name
Invoke-WebRequest -Uri $download.browser_download_url -OutFile $msi

# Install downloaded MSI
$process = Start-Process msiexec.exe -Wait -PassThru -ArgumentList "/I $msi /quiet"

if($process.ExitCode -ne 0 -or $process.ExitCode -ne 3010){
    throw "Could not install. The installer returned error code $($process.ExitCode).`r`n" + 
    "See more: https://learn.microsoft.com/en-us/windows/win32/msi/error-codes"
}

# Cleanup
Remove-Item $msi