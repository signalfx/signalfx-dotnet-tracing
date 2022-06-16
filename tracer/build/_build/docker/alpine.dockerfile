ARG DOTNETSDK_VERSION
FROM mcr.microsoft.com/dotnet/sdk:$DOTNETSDK_VERSION-alpine3.14 as base

# SECURITY NOTE: Exception is made for APK packages, 
# no need to lock versions
RUN apk update \
    && apk upgrade \
    && apk add --no-cache --update \
        clang \
        cmake \
        git \
        bash \
        make \
        alpine-sdk \
        ruby \
        ruby-dev \
        ruby-etc \
        util-linux-dev \
        autoconf \
        libtool \
        automake \
        xz-dev \
        gdb \
        musl-dbg \
    && gem install --no-document fpm --version 1.14.1

ENV IsAlpine=true

FROM base as releaser
COPY . /project
WORKDIR /project

FROM base as builder

# Copy the build project in and build it
WORKDIR /project
COPY . /build
RUN dotnet build /build

FROM base as tester

# Install .NET Core runtimes using install script
RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "760596d2e182e0edf05f4a10bd0fa266086d700fdd96438d2663f28e538912e3  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh --runtime aspnetcore --version 2.1.30 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh --runtime aspnetcore --version 3.0.3 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh --runtime aspnetcore --version 3.1.23 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh --runtime aspnetcore --version 5.0.15 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

# Copy the build project in and build it
WORKDIR /project
COPY . /build
RUN dotnet build /build
