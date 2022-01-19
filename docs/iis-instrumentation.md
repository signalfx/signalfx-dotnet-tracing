# Instrument an ASP.NET application deployed on IIS

## Instrument an ASP.NET 4.x application

By default, all ASP.NET 4.x application deployed to IIS are instrumented.
The installer enables IIS instrumentation for .NET Framework
by setting the `Environment` registry key for W3SVC and WAS services
located in the `HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services` folder.

Edit the `web.config` file of your application to add the required settings:

```xml
<configuration>
  <appSettings>
    <add key="SIGNALFX_SERVICE_NAME" value="my-service-name" />
    <add key="SIGNALFX_ENV" value="development" />
  </appSettings>
</configuration>
```

## Instrument an ASP.NET Core application

Add following [`environmentVariable`](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/web-config#set-environment-variables)
elements to the `web.config` file of your application
to set the required settings:

```xml
<configuration>
  <system.webServer>
    <aspNetCore ... >
      <environmentVariables>
        <environmentVariable name="CORECLR_ENABLE_PROFILING" value="1" />
        <environmentVariable name="CORECLR_PROFILER" value="{B4C89B0F-9908-4F73-9F59-0D77C5A06874}" />
        <environmentVariable name="SIGNALFX_SERVICE_NAME" value="my-service-name" />
        <environmentVariable name="SIGNALFX_ENV" value="development" />
      </environmentVariables>
    </aspNetCore>
  </system.webServer>
</configuration>
```
