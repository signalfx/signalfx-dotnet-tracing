﻿ARG DOTNETSDK_VERSION
ARG RUNTIME_IMAGE

# Build the ASP.NET Core app using the latest SDK
FROM mcr.microsoft.com/dotnet/sdk:$DOTNETSDK_VERSION-bullseye-slim as builder

# Build the smoke test app
WORKDIR /src
COPY ./test/test-applications/regression/AspNetCoreSmokeTest/ .

ARG PUBLISH_FRAMEWORK
RUN dotnet publish "AspNetCoreSmokeTest.csproj" -c Release --framework $PUBLISH_FRAMEWORK -o /src/publish

FROM $RUNTIME_IMAGE AS publish

WORKDIR /app

# Copy the installer files from tracer/test/test-applications/regression/AspNetCoreSmokeTest/artifacts
COPY --from=builder /src/artifacts /app/install

ARG INSTALL_CMD
RUN mkdir -p /opt/signalfx \
    && mkdir -p /var/log/signalfx \
    && cd /app/install \
    && $INSTALL_CMD \
    && rm -rf /app/install

# Set the required env vars
ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER={B4C89B0F-9908-4F73-9F59-0D77C5A06874}
ENV CORECLR_PROFILER_PATH=/opt/signalfx/SignalFx.Tracing.ClrProfiler.Native.so
ENV SIGNALFX_DOTNET_TRACER_HOME=/opt/signalfx
ENV LD_PRELOAD=/opt/signalfx/continuousprofiler/Datadog.Linux.ApiWrapper.x64.so
ENV SIGNALFX_PROFILING_ENABLED=1

ENV ASPNETCORE_URLS=http://localhost:5000

# Copy the app across
COPY --from=builder /src/publish /app/.

ENTRYPOINT ["dotnet", "AspNetCoreSmokeTest.dll"]