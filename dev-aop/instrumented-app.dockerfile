# TODO: local base image, to be replaced
FROM eshop

# Set up SignalFx APM
# TODO: update with release
ARG TRACER_VERSION=0.2.4
RUN mkdir -p /var/log/signalfx
RUN mkdir -p /opt/signalfx

RUN apt-get update \
    && apt-get -y upgrade \
    && DEBIAN_FRONTEND=noninteractive apt-get install -y --fix-missing \
    curl

RUN curl -LO https://github.com/signalfx/signalfx-dotnet-tracing/releases/download/v${TRACER_VERSION}/signalfx-dotnet-tracing_${TRACER_VERSION}_amd64.deb
RUN dpkg -i ./signalfx-dotnet-tracing_${TRACER_VERSION}_amd64.deb

ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER={B4C89B0F-9908-4F73-9F59-0D77C5A06874}
ENV CORECLR_PROFILER_PATH=/opt/signalfx/SignalFx.Tracing.ClrProfiler.Native.so
ENV SIGNALFX_DOTNET_TRACER_HOME=/opt/signalfx

# Enable AlwaysOnProfiling
ENV SIGNALFX_PROFILER_ENABLED=1
