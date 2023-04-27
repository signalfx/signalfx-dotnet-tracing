# We used a fixed, older version of debian for linking reasons
FROM mcr.microsoft.com/dotnet/runtime-deps:5.0.17-buster-slim as base
ARG DOTNETSDK_VERSION

# Based on https://github.com/dotnet/dotnet-docker/blob/34c81d5f9c8d56b36cc89da61702ccecbf00f249/src/sdk/6.0/bullseye-slim/amd64/Dockerfile
# and https://github.com/dotnet/dotnet-docker/blob/1eab4cad6e2d42308bd93d3f0cc1f7511ac75882/src/sdk/5.0/buster-slim/amd64/Dockerfile
ENV \
    # Unset ASPNETCORE_URLS from aspnet base image
    ASPNETCORE_URLS= \
    # Do not generate certificate
    DOTNET_GENERATE_ASPNET_CERTIFICATE=false \
    # Do not show first run text
    DOTNET_NOLOGO=true \
    # Enable correct mode for dotnet watch (only mode supported in a container)
    DOTNET_USE_POLLING_FILE_WATCHER=true \
    # Skip extraction of XML docs - generally not useful within an image/container - helps performance
    NUGET_XMLDOC_MODE=skip

RUN apt-get update \
    && apt-get -y upgrade \
    && DEBIAN_FRONTEND=noninteractive apt-get install -y --fix-missing \
        git=1:2.20.1-2+deb10u3 \
        procps=2:3.3.15-2 \
        wget=1.20.1-1.1 \
        curl=7.64.0-4+deb10u6 \
        cmake=3.13.4-1 \
        make=4.2.1-1.2 \
        llvm=1:7.0-47 \
        clang=1:7.0-47 \
        gcc=4:8.3.0-1 \
        build-essential=12.6 \
        rpm=4.14.2.1+dfsg1-1 \
        ruby=1:2.5.1 \
        ruby-dev=1:2.5.1 \
        rubygems-integration=1.11+deb10u1 \
        uuid-dev=2.33.1-0.1 \
        autoconf=2.69-11 \
        libtool=2.4.6-9 \
        liblzma-dev=5.2.4-1+deb10u1 \
        gdb=8.2.1-2+b3 \
    && gem install --version 1.6.0 --user-install git \
    && gem install --version 2.7.6 dotenv \
    && gem install --version 1.14.1 --minimal-deps --no-document fpm \
    && rm -rf /var/lib/apt/lists/*

# Install the .NET SDK
RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh  \
    && echo "SHA256: $(sha256sum dotnet-install.sh)" \
    && echo "3d5a87bc29fb96e8dac8c2f88d95ff619c3a921903b4c9ff720e07ca0906d55e  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh --version $DOTNETSDK_VERSION --install-dir /usr/share/dotnet \
    && rm ./dotnet-install.sh \
    && ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet \
# Trigger first run experience by running arbitrary cmd
    && dotnet help

ENV CXX=clang++
ENV CC=clang

FROM base as releaser
COPY . /project
WORKDIR /project

FROM base as builder

# Copy the build project in and build it
COPY . /build
RUN dotnet build /build
WORKDIR /project

FROM base as tester

# Install ASP.NET Core runtimes using install script
# There is no arm64 runtime available for .NET Core 2.1, so just install the .NET Core runtime in that case

RUN if [ "$(uname -m)" = "x86_64" ]; \
    then export NETCORERUNTIME2_1=aspnetcore; \
    else export NETCORERUNTIME2_1=dotnet; \
    fi \
    && curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA256: $(sha256sum dotnet-install.sh)" \
    && echo "3d5a87bc29fb96e8dac8c2f88d95ff619c3a921903b4c9ff720e07ca0906d55e  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh --runtime $NETCORERUNTIME2_1 --version 2.1.30 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh --runtime aspnetcore --version 3.0.3 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh --runtime aspnetcore --version 3.1.31 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh --runtime aspnetcore --version 5.0.17 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh --runtime aspnetcore --version 6.0.11 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh


# Copy the build project in and build it
COPY . /build
RUN dotnet build /build
WORKDIR /project
