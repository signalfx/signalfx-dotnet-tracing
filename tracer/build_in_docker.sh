#!/usr/bin/env bash
set -euox pipefail

# in case we are being run from outside this directory
cd "$(dirname "$0")"

ROOT_DIR="$(dirname $(pwd))"
BUILD_DIR="$ROOT_DIR/tracer/build/_build"
IMAGE_NAME="signalfx-dotnet-tracing/debian-base"

docker build \
   --build-arg DOTNETSDK_VERSION=7.0.406 \
   --tag $IMAGE_NAME \
   --file "$BUILD_DIR/docker/centos7.dockerfile" \
   "$BUILD_DIR"

docker run -it --rm \
    --mount type=bind,source="$ROOT_DIR",target=/project \
    --env NugetPackageDirectory=/project/packages \
    --env tracerHome=/project/shared/bin/monitoring-home/tracer \
    --env artifacts=/project/tracer/bin/artifacts \
    --env SIGNALFX_INSTRUMENTATION_TELEMETRY_ENABLED=0 \
    -p 5003:5003 \
    -v /ddlogs:/var/log/signalfx/dotnet \
    $IMAGE_NAME \
    dotnet /build/bin/Debug/_build.dll "$@"
