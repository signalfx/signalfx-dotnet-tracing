# Instrument an ASP.NET application deployed on IIS

By default, the installer enables IIS instrumentation for .NET Framework
by setting the `Environment` registry key for W3SVC and WAS services
located in the `HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services` folder.

## Additional local configuration

Additional local configuration is necessary if you are running multiple
applications on a single server (such as IIS server with multiple webapps).
This helps set application-specific behavior (such as setting the service name via `SIGNALFX_SERVICE_NAME`).
Local configuration has higher precedence than global (machine-level)
configuration and will overwrite global values.
Note that machine-level environment values affect every .NET application
on the server where they're defined.

### .NET Framework

Local configuration for .NET Framework apps can be edited
by updating the `web.config` file.
See [advanced-config.md](advanced-config.md#configuration-methods) for more information.

### .NET and .NET Core

See the [Microsoft guide](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/environments?view=aspnetcore-3.1#set-the-environment)
on how to set additional local environment variables for .NET Core apps.
