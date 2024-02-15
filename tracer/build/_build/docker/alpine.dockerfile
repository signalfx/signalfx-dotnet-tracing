ARG DOTNETSDK_VERSION
FROM mcr.microsoft.com/dotnet/sdk:$DOTNETSDK_VERSION-alpine3.16 as base

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
    && gem install --version 1.6.0 --user-install git \
    && gem install --version 2.7.6 dotenv \
    && gem install --version 1.14.1 --minimal-deps --no-document fpm

ENV IsAlpine=true

FROM base as releaser
COPY . /project
WORKDIR /project

FROM base as builder

WORKDIR /project

FROM base as tester

# Install .NET Core runtimes using install script
RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA256: $(sha256sum dotnet-install.sh)" \
    && echo "ff2ff47318ffff0af9a6395ca6308b54376fba8c97a3a5eba3e848b0caf17b58  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh --runtime aspnetcore --version 2.1.30 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh --runtime aspnetcore --version 3.1.31 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh --runtime aspnetcore --version 6.0.11 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
