#!/bin/bash

set -euo pipefail

if [[ $# -lt 1 ]]
then
    cat <<EOF
    Usage: ${0} TILE_VERSION
    This script requires a single parameter: new version of the tile.
EOF
    exit 1
fi

TILE_VERSION="$1"
TILE_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BUILDPACK_LINUX_RELEASE="${TILE_DIR}/resources/signalfx_dotnet_tracing_buildpack-linux.zip"
BUILDPACK_WINDOWS_RELEASE="${TILE_DIR}/resources/signalfx_dotnet_tracing_buildpack-windows.zip"

# clean old release
rm -rf release product
# clean old buildpacks
rm -f resources/*.zip

# build the linux buildpack
(cd "${TILE_DIR}/../buildpack-linux" && \
    echo "$TILE_VERSION" > VERSION && \
    zip "$BUILDPACK_LINUX_RELEASE" bin/* manifest.yml README.md VERSION)

# build the windows buildpack
(cd "${TILE_DIR}/../buildpack-windows" && \
    echo "$TILE_VERSION" > VERSION && \
    ./build.sh && \
    zip "$BUILDPACK_WINDOWS_RELEASE" bin/* manifest.yml README.md VERSION)

# build the tile
tile build "$TILE_VERSION"
