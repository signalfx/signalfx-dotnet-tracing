# Default Service Name

If the default service name doesn't fit well with your usage or naming conventions configure
the [`SIGNALFX_SERVICE_NAME`](advanced-config.md) setting.
The default service name is captured via the steps below,
stopping at the first one that succeeds:

1. For the [SignalFx .NET Tracing Azure Site Extension](../shared/src/azure-site-extension/README.md)
the default service name is the __site name__ as defined by the
[WEBSITE_SITE_NAME](https://docs.microsoft.com/en-us/azure/app-service/reference-app-settings?tabs=kudu%2Cdotnet) 
environment variable.

2. For an ASP.NET hosted application, for example IIS .NET Framework apps, the default service name is `SiteName[/VirtualPath]`.

3. For other applications the name of the [entry assembly](https://docs.microsoft.com/en-us/dotnet/api/system.reflection.assembly.getentryassembly?view=net-6.0)
is used as the default service name. Typically this is the name of your .NET project file.

4. If the [entry assembly is not available](https://docs.microsoft.com/en-us/dotnet/api/system.reflection.assembly.getentryassembly?view=net-6.0#remarks).
The instrumentation tries to use the current process name.
The process name can be `dotnet` if it is launched directly using an assembly, eg.: `dotnet InstrumentedApp.dll`.

5. If all steps above fail the default service name used is `UnknownService`.
