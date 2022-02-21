FROM public.ecr.aws/lambda/dotnet:core3.1

# Add Tracer
# ML: parameterize
COPY ./src/bin/windows-tracer-home /opt/signalfx

# Add Tests
COPY ./src/bin/windows-tracer-home/createLogPath.sh ./test/test-applications/integrations/Samples.AWS.Lambda/bin/Release/netcoreapp3.1/*.dll /var/task/
COPY ./src/bin/windows-tracer-home/createLogPath.sh ./test/test-applications/integrations/Samples.AWS.Lambda/bin/Release/netcoreapp3.1/*.deps.json /var/task/
COPY ./src/bin/windows-tracer-home/createLogPath.sh ./test/test-applications/integrations/Samples.AWS.Lambda/bin/Release/netcoreapp3.1/*.runtimeconfig.json /var/task/

ENV SIGNALFX_LOG_LEVEL="DEBUG"
ENV SIGNALFX_TRACE_ENABLED=true
ENV SIGNALFX_DOTNET_TRACER_HOME="/opt/signalfx"
ENV SIGNALFX_INTEGRATIONS="/opt/signalfx/integrations.json"
ENV _SIGNALFX_EXTENSION_PATH="/opt/signalfx/SignalFx.Tracing.ClrProfiler.Native.so"

ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER="{B4C89B0F-9908-4F73-9F59-0D77C5A06874}"
ENV CORECLR_PROFILER_PATH="/opt/signalfx/SignalFx.Tracing.ClrProfiler.Native.so"

ENV AWS_LAMBDA_FUNCTION_NAME="my-test-function"