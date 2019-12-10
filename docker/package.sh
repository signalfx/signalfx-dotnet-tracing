#!/bin/bash
# Modified by SignalFx
set -euxo pipefail

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null && pwd )"
VERSION=1.10.0

mkdir -p $DIR/../deploy/linux
cp $DIR/../integrations.json $DIR/../src/Datadog.Trace.ClrProfiler.Native/bin/Debug/x64/

cd $DIR/../deploy/linux
for pkgtype in deb rpm tar ; do
    fpm \
        -f \
        -s dir \
        -t $pkgtype \
        -n signalfx-dotnet-tracing \
        -v $VERSION \
        $(if [ $pkgtype != 'tar' ] ; then echo --prefix /opt/signalfx-dotnet-tracing ; fi) \
        --chdir $DIR/../src/Datadog.Trace.ClrProfiler.Native/bin/Debug/x64 \
        netstandard2.0/ \
        SignalFx.Tracing.ClrProfiler.Native.so \
        integrations.json
done

gzip -f signalfx-dotnet-tracing.tar
mv signalfx-dotnet-tracing.tar.gz signalfx-dotnet-tracing-$VERSION.tar.gz
