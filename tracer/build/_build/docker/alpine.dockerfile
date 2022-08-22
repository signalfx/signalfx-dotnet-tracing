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
    && gem install --version 1.6.0 --user-install git \
    && gem install --version 2.7.6 dotenv \
    && gem install --version 1.14.1 --minimal-deps --no-document fpm

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
    && echo "18450c72b35727b13b9b8a4191c2c95d7fbd3f4a0091ab71edcfb16eb57e5494  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh --runtime aspnetcore --version 2.1.30 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh --runtime aspnetcore --version 3.0.3 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh --runtime aspnetcore --version 3.1.26 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh --runtime aspnetcore --version 5.0.17 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

# Copy the build project in and build it
WORKDIR /project
COPY . /build
RUN dotnet build /build
