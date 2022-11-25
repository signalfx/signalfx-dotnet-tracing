# Windows Nano Server

Setting up SignalFx Instrumentation for .NET to run in Windows Nano Server Docker image.

```Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:6.0-nanoserver-ltsc2022

# Download SignalFx Instrumentation for .NET zip.
ARG TRACER_VERSION=0.2.10
ADD https://github.com/signalfx/signalfx-dotnet-tracing/releases/download/v${TRACER_VERSION}/signalfx-dotnet-tracing-${TRACER_VERSION}.zip "C:/signalfx-dotnet-tracing.zip"

# Extract zip to C:/signalfx/
# Note: Windows Nano Server doesn't have capabilities to use MSI, so we are using zip here.
RUN mkdir "C:/signalfx"
RUN tar -xf "C:/signalfx-dotnet-tracing.zip" -C "C:/signalfx/"
RUN del "C:/signalfx-dotnet-tracing.zip"

# Setup global SignalFx environment variables using the unzipping path 'C:/signalfx'.
ENV CORECLR_ENABLE_PROFILING=1 \
    CORECLR_PROFILER='{B4C89B0F-9908-4F73-9F59-0D77C5A06874}' \
    CORECLR_PROFILER_PATH_32='C:/signalfx/win-x86/SignalFx.Tracing.ClrProfiler.Native.dll' \
    CORECLR_PROFILER_PATH_64='C:/signalfx/win-x64/SignalFx.Tracing.ClrProfiler.Native.dll' \
    SIGNALFX_DOTNET_TRACER_HOME='C:/signalfx/' \
    SIGNALFX_ENDPOINT_URL='<my-endpoint-here>'

# Copy your application contents.
COPY ./app ./app

WORKDIR /app

# Specify your dotnet app's entrypoint.
ENTRYPOINT ["dotnet", "MyEntryPoint.dll"]
```

# Windows Server Core

Setting up SignalFx Instrumentation for .NET to run in Windows Server Core Docker image.

```Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:6.0-windowsservercore-ltsc2022

SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

# Setup global SignalFx environment variables.
# Important!: Enabling instrumentation here will instrument all of the .NET applications running on the Windows Server Core.
# Whenever possible reduce the CORECLR_ENABLE_PROFILING environment variable's scope from global to the application level.
ENV CORECLR_ENABLE_PROFILING=1 \
    CORECLR_PROFILER='{B4C89B0F-9908-4F73-9F59-0D77C5A06874}' \
    SIGNALFX_ENDPOINT_URL='<my-endpoint-here>' \

ARG TRACER_VERSION=0.2.10

# Download and install SignalFx Instrumentation for .NET MSI.
# Note: MSI is preferred installation method for Windows to setup profiler at once and reduce manual steps.
RUN New-Item -Path 'C:\Temp' -ItemType Directory
RUN Invoke-WebRequest "https://github.com/signalfx/signalfx-dotnet-tracing/releases/download/v${env:TRACER_VERSION}/signalfx-dotnet-tracing-${env:TRACER_VERSION}-x64.msi" -OutFile "C:\Temp\signalfx-dotnet-tracing.msi"
RUN msiexec.exe /i "C:\Temp\signalfx-dotnet-tracing.msi" /QN /L*V "C:\Temp\signalfx-msi.log"
RUN Remove-Item "C:\Temp\signalfx-dotnet-tracing.msi"

# Copy your application contents.
COPY ./app ./app

WORKDIR /app
ENTRYPOINT ["dotnet", "MyEntryPoint.dll"]
```
