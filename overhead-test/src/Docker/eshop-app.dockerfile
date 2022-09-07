FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

RUN git clone https://github.com/dotnet-architecture/eShopOnWeb.git
RUN git -C ./eShopOnWeb checkout ce63e38a23046f946888da47cf8fe579dd7f3b2a

# disable auth for item creation
RUN sed -i -e '15d' /eShopOnWeb/src/PublicApi/CatalogItemEndpoints/Create.cs

# disable auth for item deletion
RUN sed -i -e '13d' /eShopOnWeb/src/PublicApi/CatalogItemEndpoints/Delete.cs

WORKDIR /eShopOnWeb/src/PublicApi

RUN dotnet restore
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS baseline-app

WORKDIR /app

RUN apt-get update \
    && apt-get -y upgrade \
    && DEBIAN_FRONTEND=noninteractive apt-get install -y --fix-missing \
    curl

# TODO Splunk: for now install dotnet-counters in app (replace with sidecar container)
RUN curl -sSL https://aka.ms/dotnet-counters/linux-x64 --output dotnet-counters  \
    && chmod +x ./dotnet-counters

COPY --from=build /eShopOnWeb/src/PublicApi/out ./
ENTRYPOINT ["dotnet", "PublicApi.dll"]

FROM baseline-app as instrumented-app

# Set up SignalFx APM

RUN mkdir -p /var/log/signalfx
RUN mkdir -p /opt/signalfx

COPY package.deb ./
RUN dpkg -i package.deb

ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER={B4C89B0F-9908-4F73-9F59-0D77C5A06874}
ENV CORECLR_PROFILER_PATH=/opt/signalfx/SignalFx.Tracing.ClrProfiler.Native.so
ENV SIGNALFX_DOTNET_TRACER_HOME=/opt/signalfx
