FROM mcr.microsoft.com/dotnet/core/runtime:2.1-stretch-slim AS base
ARG TRACER_VERSION=0.2.4
RUN mkdir -p /opt/signalfx
RUN mkdir -p /var/log/signalfx/dotnet
RUN curl -L https://github.com/signalfx/signalfx-dotnet-tracing/releases/download/v$TRACER_VERSION/signalfx-dotnet-tracing-$TRACER_VERSION.tar.gz | tar xzf - -C /opt/signalfx

ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER={B4C89B0F-9908-4F73-9F59-0D77C5A06874}
ENV CORECLR_PROFILER_PATH=/opt/signalfx/SignalFx.Tracing.ClrProfiler.Native.so
ENV SIGNALFX_DOTNET_TRACER_HOME=/opt/signalfx
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