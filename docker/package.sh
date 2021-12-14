#!/bin/bash
# Modified by SignalFx
set -euxo pipefail

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null && pwd )"
VERSION=0.1.16

mkdir -p $DIR/../deploy/linux
for target in integrations.json defaults.env LICENSE NOTICE createLogPath.sh ; do
    cp $DIR/../$target $DIR/../src/Datadog.Trace.ClrProfiler.Native/bin/Debug/x64/
done

# leave OT dependency to user app
OPENTRACING_DLL=$DIR/../src/Datadog.Trace.ClrProfiler.Native/bin/Debug/x64/netstandard2.0/OpenTracing.dll
if [ -f $OPENTRACING_DLL ]; then
    mv $OPENTRACING_DLL /tmp/OpenTracing.dll
fi

cd $DIR/../deploy/linux
for pkgtype in $PKGTYPES ; do
    fpm \
        -f \
        -s dir \
        -t $pkgtype \
        -n signalfx-dotnet-tracing \
        -m signalfx-oss@splunk.com \
        --license "Apache License, Version 2.0" \
        --provides signalfx-dotnet-tracing \
        --vendor SignalFx \
        --url "https://docs.signalfx.com/en/latest/apm/apm-instrument/apm-dotnet.html" \
        -v $VERSION \
        --prefix /opt/signalfx-dotnet-tracing \
        --chdir $DIR/../src/Datadog.Trace.ClrProfiler.Native/bin/Debug/x64 \
        netstandard2.0/ \
        SignalFx.Tracing.ClrProfiler.Native.so \
        integrations.json \
        createLogPath.sh \
        defaults.env \
        LICENSE \
        NOTICE
done

if [ -f /tmp/OpenTracing.dll ]; then
    mv /tmp/OpenTracing.dll $OPENTRACING_DLL
fi

gzip -f signalfx-dotnet-tracing.tar

if [ -z "${MUSL-}" ]; then
  mv signalfx-dotnet-tracing.tar.gz signalfx-dotnet-tracing-$VERSION.tar.gz
else
  mv signalfx-dotnet-tracing.tar.gz signalfx-dotnet-tracing-$VERSION-musl.tar.gz
fi
