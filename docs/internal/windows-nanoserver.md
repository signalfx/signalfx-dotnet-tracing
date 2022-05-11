# Windows nanoserver

Setting up SignalFx Instrumentation for .NET to run in Windows Nanoserver Docker image.

```Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:6.0.3-nanoserver-ltsc2022

# Setup Signalfx environment variables
ENV CORECLR_ENABLE_PROFILING=1 \
    CORECLR_PROFILER='{B4C89B0F-9908-4F73-9F59-0D77C5A06874}' \
    CORECLR_PROFILER_PATH_32='C:/signalfx/win-x86/SignalFx.Tracing.ClrProfiler.Native.dll' \
    CORECLR_PROFILER_PATH_64='C:/signalfx/win-x64/SignalFx.Tracing.ClrProfiler.Native.dll' \
    SIGNALFX_DOTNET_TRACER_HOME='C:/signalfx/' \
    SIGNALFX_ENDPOINT_URL='<my-endpoint-here>'

# Download SignalFx Instrumentation for .NET zip
ARG TRACER_VERSION=0.2.4
ADD https://github.com/signalfx/signalfx-dotnet-tracing/releases/download/v${TRACER_VERSION}/signalfx-dotnet-tracing-${TRACER_VERSION}.zip "C:/signalfx-dotnet-tracing.zip"

# Extract zip to C:/signalfx/
RUN mkdir "C:/signalfx"
RUN tar -xf "C:/signalfx-dotnet-tracing.zip" -C "C:/signalfx/"
RUN del "C:/signalfx-dotnet-tracing.zip"

# Copy your application contents
COPY ./app ./app

WORKDIR /app

# Specify your dotnet app's entrypoint
ENTRYPOINT ["dotnet", "MyEntryPoint.dll"]
```