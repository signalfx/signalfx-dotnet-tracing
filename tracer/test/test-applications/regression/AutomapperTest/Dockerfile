FROM mcr.microsoft.com/dotnet/core/runtime:2.1-stretch-slim AS base
ARG TRACER_VERSION=0.0.1
RUN mkdir -p /opt/signalfx
RUN mkdir -p /var/log/signalfx/dotnet
RUN curl -L https://github.com/signalfx/signalfx-dotnet-tracing/releases/download/v$TRACER_VERSION/signalfx-dotnet-tracing-$TRACER_VERSION.tar.gz | tar xzf - -C /opt/signalfx

ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER={918728DD-259F-4A6A-AC2B-B85E1B658318}
ENV CORECLR_PROFILER_PATH=/opt/signalfx/SignalFx.Instrumentation.ClrProfiler.Native.so
ENV SIGNALFX_DOTNET_TRACER_HOME=/opt/signalfx
ENV SIGNALFX_INTEGRATIONS=/opt/signalfx/integrations.json
ENV SIGNALFX_TRACE_ENABLED=true

FROM mcr.microsoft.com/dotnet/core/sdk:2.1-stretch AS build
WORKDIR "/src"
COPY ["reproductions/AutomapperTest/AutomapperTest.csproj", "/src/reproductions/AutomapperTest/"]
COPY ["src/Datadog.Trace/Datadog.Trace.csproj", "/src/src/Datadog.Trace/"]
RUN dotnet restore "/src/reproductions/AutomapperTest/AutomapperTest.csproj"
COPY . .
WORKDIR "/src/reproductions/AutomapperTest"
RUN dotnet build "AutomapperTest.csproj" -c Release -o /app

FROM build as publish
RUN dotnet publish "AutomapperTest.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "AutomapperTest.dll"]