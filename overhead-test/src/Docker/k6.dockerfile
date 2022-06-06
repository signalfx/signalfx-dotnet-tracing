FROM grafana/k6:latest
# workaround for current limitation in dotnet testcontainers - override user to create results file in mounted dir
USER root