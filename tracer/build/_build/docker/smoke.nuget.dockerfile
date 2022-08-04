ARG DOTNETSDK_VERSION
ARG RUNTIME_IMAGE

# Build the ASP.NET Core app using the latest SDK
FROM mcr.microsoft.com/dotnet/sdk:$DOTNETSDK_VERSION-bullseye-slim as builder

# Build the smoke test app
WORKDIR /src
COPY ./test/test-applications/regression/AspNetCoreSmokeTest/ .

# do an initial restore
RUN dotnet restore "AspNetCoreSmokeTest.csproj"

# install the package
RUN dotnet add package "Datadog.Monitoring.Distribution" --prerelease --source /src/artifacts

ARG PUBLISH_FRAMEWORK
RUN dotnet publish "AspNetCoreSmokeTest.csproj" -c Release --framework $PUBLISH_FRAMEWORK -o /src/publish

FROM $RUNTIME_IMAGE AS publish

WORKDIR /app

RUN mkdir -p /opt/signalfx \
    && mkdir -p /var/log/signalfx 

# Copy the app across
COPY --from=builder /src/publish /app/.

ARG RELATIVE_PROFILER_PATH

# Set the required env vars
ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER={B4C89B0F-9908-4F73-9F59-0D77C5A06874}
ENV CORECLR_PROFILER_PATH=/app/${RELATIVE_PROFILER_PATH}
ENV SIGNALFX_DOTNET_TRACER_HOME=/app/signalfx
ENV SIGNALFX_PROFILING_ENABLED=1

ENV ASPNETCORE_URLS=http://localhost:5000

ENTRYPOINT ["dotnet", "AspNetCoreSmokeTest.dll"]