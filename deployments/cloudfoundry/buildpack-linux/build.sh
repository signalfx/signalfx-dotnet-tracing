#!/bin/bash

set -euo pipefail

BUILDPACK_RELEASE="signalfx_dotnet_tracing_buildpack-linux.zip"

zip "$BUILDPACK_RELEASE" bin/* manifest.yml README.md
