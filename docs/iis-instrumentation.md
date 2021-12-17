# IIS instrumentation

By default Windows installer enables IIS instrumentation by setting `Environment` registry key for W3SVC and WAS services located in `HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services` folder.

## Additional local configuration

Additional local configuration is necessary if you are running multiple applications on a single server (such as IIS server with multiple web apps). This helps to set application specific behavior (such as setting service name via `SIGNALFX_SERVICE_NAME`). Local configuration has higher precedence than global (machine level) configuration and will overwrite global values. Please note that machine level environment values are affecting every .NET application on that server.

### .NET Framework

Local configuration for .NET Framework apps can be done via updating `web.config`. See [advanced-config.md](advanced-config.md#configuration-methods) for syntax.

### .NET Core

Please follow [Microsoft guide](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/environments?view=aspnetcore-3.1#set-the-environment) how to set additional local environment variables for .NET Core apps.