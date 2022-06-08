FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build-env

RUN git clone https://github.com/dotnet-architecture/eShopOnWeb.git
RUN git -C ./eShopOnWeb checkout e5e9868003b2e940c731cdf34a3b8fa2a89d272c

# modify the code to use real database, instead of in-memory, as outlined in:
# https://github.com/dotnet-architecture/eShopOnWeb/tree/e5e9868003b2e940c731cdf34a3b8fa2a89d272c#configuring-the-sample-to-use-sql-server

# remove line 37
RUN sed -i -e '37d' /eShopOnWeb/src/Web/Startup.cs

# uncomment line 39
RUN sed -i -e '39s/\/\///g' /eShopOnWeb/src/Web/Startup.cs

# uncomment line 19, and fix the name for the variable
RUN sed -i -e '19s/\/\/\ context/catalogContext/' /eShopOnWeb/src/Infrastructure/Data/CatalogContextSeed.cs

WORKDIR /eShopOnWeb/src/Web

RUN dotnet restore
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS baseline-app

WORKDIR /app

# TODO Splunk: for now install dotnet-counters in app (replace with sidecar container)
RUN curl -sSL https://aka.ms/dotnet-counters/linux-x64 --output dotnet-counters  \
    && chmod +x ./dotnet-counters

COPY --from=build-env /eShopOnWeb/src/Web/out ./

ENTRYPOINT ["dotnet", "Web.dll"]

FROM baseline-app as instrumented-app

# Set up SignalFx APM

RUN mkdir -p /var/log/signalfx
RUN mkdir -p /opt/signalfx

RUN apt-get update \
    && apt-get -y upgrade \
    && DEBIAN_FRONTEND=noninteractive apt-get install -y --fix-missing \
    curl

# TODO Splunk: update with release
ARG TRACER_VERSION=0.2.4

RUN curl -LO https://github.com/signalfx/signalfx-dotnet-tracing/releases/download/v${TRACER_VERSION}/signalfx-dotnet-tracing_${TRACER_VERSION}_amd64.deb
RUN dpkg -i ./signalfx-dotnet-tracing_${TRACER_VERSION}_amd64.deb

ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER={B4C89B0F-9908-4F73-9F59-0D77C5A06874}
ENV CORECLR_PROFILER_PATH=/opt/signalfx/SignalFx.Tracing.ClrProfiler.Native.so
ENV SIGNALFX_DOTNET_TRACER_HOME=/opt/signalfx
