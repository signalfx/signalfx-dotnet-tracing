#!/bin/bash
# Modified by SignalFx
set -euxo pipefail

mkdir -p /var/log/signalfx/dotnet
touch /var/log/signalfx/dotnet/dotnet-profiler.log
tail -f /var/log/signalfx/dotnet/dotnet-profiler.log | awk '
  /info/ {print "\033[32m" $0 "\033[39m"}
  /warn/ {print "\033[31m" $0 "\033[39m"}
' &

eval "$@"
