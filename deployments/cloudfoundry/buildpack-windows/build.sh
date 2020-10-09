#!/bin/bash

set -euo pipefail

export GOPATH=$PWD

GOOS=windows go build -ldflags="-s -w" -o bin/supply.exe supply
