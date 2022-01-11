# Azure services instrumentation guide

## App Service

1. Choose your App Service.
2. Navigate to `Development Tools` > `Extensions`.
3. Find and install the `SignalFx .NET Tracing` extension.
4. Navigate to `Settings` > `Configuration`.
5. Add `New application setting`s to configur the receiver: 

    ```
    Name: SIGNALFX_ACCESS_TOKEN 
    Value: (Your SIGNALFX access token)

    Name: SIGNALFX_ENDPOINT_URL
    Value: (Your Collector or SignalFX ingest endpoint)
    ```
    (See [advanced-config.md](advanced-config.md) for more options.)
6. Make sure that App Service was restarted.


## WebJobs (Experimental)

Repeat all of the App Service section steps. Currently environment configuration must be done manually. Specially notice that `SIGNALFX_AZURE_APP_SERVICES` must be disabled. It is strongly adviced to have a separate App Service for the Webjobs.

    ```
    ## Enables .NET Framework instrumentation

    Name: COR_ENABLE_PROFILING
    Value: 1

    Name: COR_PROFILER
    Value: {B4C89B0F-9908-4F73-9F59-0D77C5A06874}

    ## version must match SignalFx .NET Tracing extension version
    Name: COR_PROFILER_PATH
    Value: C:\home\signalfx\tracing\v0.2.0\x64\SignalFx.Tracing.ClrProfiler.Native.dll

    ## version must match SignalFx .NET Tracing extension version
    Name: COR_PROFILER_PATH_32
    Value: C:\home\signalfx\tracing\v0.2.0\x86\SignalFx.Tracing.ClrProfiler.Native.dll

    ## version must match SignalFx .NET Tracing extension version
    Name: COR_PROFILER_PATH_64
    Value: C:\home\signalfx\tracing\v0.2.0\x64\SignalFx.Tracing.ClrProfiler.Native.dll

    ## Enables .NET core instrumentation

    Name: CORECLR_ENABLE_PROFILING
    Value: 1

    Name: CORECLR_PROFILER
    Value: {B4C89B0F-9908-4F73-9F59-0D77C5A06874}

    ## version must match SignalFx .NET Tracing extension version
    Name: CORECLR_PROFILER_PATH_32
    Value: C:\home\signalfx\tracing\v0.2.0\x86\SignalFx.Tracing.ClrProfiler.Native.dll

    ## version must match SignalFx .NET Tracing extension version
    Name: CORECLR_PROFILER_PATH_64
    Value: C:\home\signalfx\tracing\v0.2.0\x64\SignalFx.Tracing.ClrProfiler.Native.dll

    ## General variables

    ## version must match SignalFx .NET Tracing extension version
    Name: SIGNALFX_DOTNET_TRACER_HOME
    Value: C:\home\signalfx\tracing\v0.2.0

    Name: SIGNALFX_PROFILER_EXCLUDE_PROCESSES
    Value: SnapshotUploader.exe;workerforwarder.exe

    ## version must match SignalFx .NET Tracing extension version
    Name: SIGNALFX_TRACE_LOG_PATH
    Value: C:\home\LogFiles\signalfx\tracing\v0.2.0\dotnet-profiler.log

    ## necessary to enable background services instrumentation
    Name: SIGNALFX_AZURE_APP_SERVICES
    Value: 0    

    Name: SIGNALFX_ACCESS_TOKEN 
    Value: (Your SIGNALFX access token)

    Name: SIGNALFX_ENDPOINT_URL
    Value: (Your Collector or SignalFX ingest endpoint)
    ```
 
## Azure Functions

Currently not supported. 

<!-- TODO: What are the workarounds we can provide? -->
