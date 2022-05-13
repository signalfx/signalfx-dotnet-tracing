# Instrument a Windows Service running a .NET application

Configure the Windows Service so that the following environment variables are set:

```env
COR_ENABLE_PROFILING=1
COR_PROFILER={B4C89B0F-9908-4F73-9F59-0D77C5A06874}
CORECLR_ENABLE_PROFILING=1
CORECLR_PROFILER={B4C89B0F-9908-4F73-9F59-0D77C5A06874}
```

Example in PowerShell:

```powershell
$svcName = "MySrv"    # The name of the Windows Service you want to instrument
[string[]] $vars = @(
   "COR_ENABLE_PROFILING=1",                                  # Enable the .NET Framework Profiler
   "COR_PROFILER={B4C89B0F-9908-4F73-9F59-0D77C5A06874}",     # Select the .NET Framework Profiler
   "CORECLR_ENABLE_PROFILING=1",                              # Enable the .NET (Core) Profiler
   "CORECLR_PROFILER={B4C89B0F-9908-4F73-9F59-0D77C5A06874}", # Select the .NET (Core) Profiler
   "SIGNALFX_SERVICE_NAME=my-service-name",                   # Set the service name
   "SIGNALFX_ENV=production"                                  # Set the environment name
)
Set-ItemProperty HKLM:SYSTEM\CurrentControlSet\Services\$svcName -Name Environment -Value $vars
# Now each time you start the service, it will be auto-instrumented.
```
