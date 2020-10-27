#!/bin/bash

set -euo pipefail

BUILDPACK_RELEASE="signalfx_dotnet_tracing_buildpack-windows.zip"
export GOPATH=$PWD

GOOS=windows go build -ldflags="-s -w" -o bin/supply.exe supply
zip "$BUILDPACK_RELEASE" bin/* manifest.yml README.md
