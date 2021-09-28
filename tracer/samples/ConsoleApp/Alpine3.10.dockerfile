FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -f netcoreapp3.1 -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/runtime:3.1-alpine3.10
WORKDIR /app
COPY --from=build /app/out .

# Set up Datadog APM
RUN apk --no-cache update && apk add curl
<<<<<<< HEAD
ARG TRACER_VERSION=0.0.1
RUN mkdir -p /var/log/signalfx
RUN mkdir -p /opt/signalfx
RUN curl -L https://github.com/signalfx/signalfx-dotnet-tracing/releases/download/v${TRACER_VERSION}/signalfx-dotnet-tracing-${TRACER_VERSION}-musl.tar.gz \
  |  tar xzf - -C /opt/signalfx
=======
ARG TRACER_VERSION=1.28.8
RUN mkdir -p /var/log/datadog
RUN mkdir -p /opt/datadog
RUN curl -L https://github.com/DataDog/dd-trace-dotnet/releases/download/v${TRACER_VERSION}/datadog-dotnet-apm-${TRACER_VERSION}-musl.tar.gz \
  |  tar xzf - -C /opt/datadog
>>>>>>> ed0e465a7 ([Version Bump] 1.28.8 (#1827))

ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER={918728DD-259F-4A6A-AC2B-B85E1B658318}
ENV CORECLR_PROFILER_PATH=/opt/signalfx/OpenTelemetry.AutoInstrumentation.ClrProfiler.Native.so
ENV SIGNALFX_INTEGRATIONS=/opt/signalfx/integrations.json
ENV SIGNALFX_DOTNET_TRACER_HOME=/opt/signalfx

CMD ["dotnet", "ConsoleApp.dll"]