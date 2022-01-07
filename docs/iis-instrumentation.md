# Instrument an ASP.NET application deployed on IIS

By default, the installer enables IIS instrumentation for .NET Framework
by setting the `Environment` registry key for W3SVC and WAS services
located in the `HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services` folder.

For ASP.NET applications, for example IIS applications running on the .NET Framework, the
default service name is `ServiceName[\VirtualPath]`.
For ASP.NET Core applications the default service name is the entry assembly name, typically
the name of your .NET Core project.
If the defaults above don't fit well with your usage or naming conventions configure `SIGNALFX_SERVICE_NAME` as described in [advanced-config.md](advanced-config.md#configuration-methods).

Consider using `web.config` as the configuration method
to avoid potential configuration conflicts between other applications.
