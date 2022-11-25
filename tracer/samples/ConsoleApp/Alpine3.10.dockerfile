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
ARG TRACER_VERSION=1.0.0
RUN mkdir -p /var/log/signalfx
RUN mkdir -p /opt/signalfx
RUN curl -L https://github.com/signalfx/signalfx-dotnet-tracing/releases/download/v${TRACER_VERSION}/signalfx-dotnet-tracing-${TRACER_VERSION}-musl.tar.gz \
  |  tar xzf - -C /opt/signalfx

ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER={B4C89B0F-9908-4F73-9F59-0D77C5A06874}
ENV CORECLR_PROFILER_PATH=/opt/signalfx/SignalFx.Tracing.ClrProfiler.Native.so
ENV SIGNALFX_DOTNET_TRACER_HOME=/opt/signalfx

CMD ["dotnet", "ConsoleApp.dll"]