# Troubleshooting

Check if you are not hitting one of the issues listed below.

## Linux instrumentation not working

The proper binary needs to be selected when deploying to Linux,
eg.: the default Microsoft .NET images are based on Debian and should use
the `deb` package, see the [Linux](README.md#Linux) setup section.

If you are not sure what is the Linux distribution being used try the following commands:

```terminal
lsb_release -a
cat /etc/*release
cat /etc/issue*
cat /proc/version
```

## High CPU usage

The default installation of auto-instrumentation enables tracing all .NET processes
on the box.
In the typical scenarios (dedicated VMs or containers), this is not a problem.
Use the environment variables `SIGNALFX_PROFILER_EXCLUDE_PROCESSES` and `SIGNALFX_PROFILER_PROCESSES`
to include/exclude applications from the tracing auto-instrumentation.
These are ";" delimited lists that control the inclusion/exclusion of processes.

## Investigating other issues

If none of the suggestions above solves your issue, detailed logs are necessary.
Follow the steps below to get the detailed logs from
SignalFx Tracing Library for .NET.

Set the environment variable `SIGNALFX_TRACE_DEBUG` to `true` before
the instrumented process starts.
By default, the library writes the log files under the below predefined locations.
If needed, change the default location by updating the environment variable
`SIGNALFX_TRACE_LOG_DIRECTORY` to an appropriate path.
On Linux, the default log location is `/var/log/signalfx/dotnet/`
On Windows, the default log location is `%ProgramData%\SignalFx .NET Tracing\logs\`
Compress the whole folder to capture the multiple log files and send
the compressed folder to us.
After obtaining the logs, remember to remove the environment variable
`SIGNALFX_TRACE_DEBUG` to avoid unnecessary overhead.
