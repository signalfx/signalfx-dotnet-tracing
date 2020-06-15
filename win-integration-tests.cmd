@rem Simple Windows cmd file to run equivalent of integration tests
setlocal enabledelayedexpansion

set buildConfiguration=Debug
set buildPlatform=x64

@rem publishOutput MUST be the an absolute path since projects in sub-directories will use it.
set publishOutput=%~dp0src\bin\managed-publish

nuget restore Datadog.Trace.sln
if %errorlevel% neq 0 exit /b %errorlevel%

for %%i in (net45 net461 netstandard2.0) do (
    dotnet publish --configuration %buildConfiguration% --framework %%i --output %publishOutput%/%%i .\src\DataDog.Trace.ClrProfiler.Managed\DataDog.Trace.ClrProfiler.Managed.csproj
    if !errorlevel! neq 0 exit /b !errorlevel!
)

dotnet build --configuration %buildConfiguration% .\src\Datadog.Trace.ClrProfiler.Managed.Loader\Datadog.Trace.ClrProfiler.Managed.Loader.csproj
if %errorlevel% neq 0 exit /b %errorlevel%

msbuild Datadog.Trace.proj /t:BuildCpp /p:Configuration=%buildConfiguration%;Platform=%buildPlatform%
if %errorlevel% neq 0 exit /b %errorlevel%

msbuild Datadog.Trace.proj /t:BuildFrameworkReproductions /p:Configuration=%buildConfiguration%;Platform=%buildPlatform%
if %errorlevel% neq 0 exit /b %errorlevel%

for /f %%i in ('dir /s/b %publishOutput%\net45\*.dll') do  (
    gacutil /i %%i /f
    if !errorlevel! neq 0 exit /b !errorlevel!
)

for /f %%i in ('dir /s/b .\samples\*.csproj') do  (
    dotnet build --configuration %buildConfiguration% -p:Platform=%buildPlatform% -p:ManagedProfilerOutputDirectory=%publishOutput% %%i
    if !errorlevel! neq 0 exit /b !errorlevel!
)

for /f %%i in ('dir /s/b .\reproductions\*.csproj') do  (
    dotnet build --configuration %buildConfiguration% -p:Platform=%buildPlatform% -p:ManagedProfilerOutputDirectory=%publishOutput% %%i
    rem ignore some errors since some reproductions are broken
)

dotnet build --configuration %buildConfiguration% -p:Platform=%buildPlatform% -p:ManagedProfilerOutputDirectory=%publishOutput% ./test/Datadog.Trace.IntegrationTests/Datadog.Trace.IntegrationTests.csproj
if %errorlevel% neq 0 exit /b %errorlevel%

dotnet build --configuration %buildConfiguration% -p:Platform=%buildPlatform% -p:ManagedProfilerOutputDirectory=%publishOutput% ./test/Datadog.Trace.ClrProfiler.IntegrationTests/Datadog.Trace.ClrProfiler.IntegrationTests.csproj
if %errorlevel% neq 0 exit /b %errorlevel%

dotnet build --configuration %buildConfiguration% -p:Platform=%buildPlatform% -p:ManagedProfilerOutputDirectory=%publishOutput% ./test/Datadog.Trace.OpenTracing.IntegrationTests/Datadog.Trace.OpenTracing.IntegrationTests.csproj
if %errorlevel% neq 0 exit /b %errorlevel%

@rem TESTS

dotnet test --configuration %buildConfiguration% -p:Platform=%buildPlatform% .\test\Datadog.Trace.IntegrationTests\Datadog.Trace.IntegrationTests.csproj --logger trx -r .\test\Datadog.Trace.IntegrationTests\results
if %errorlevel% neq 0 exit /b %errorlevel%

dotnet test --configuration %buildConfiguration% -p:Platform=%buildPlatform% .\test\Datadog.Trace.OpenTracing.IntegrationTests\Datadog.Trace.OpenTracing.IntegrationTests.csproj --logger trx -r .\test\Datadog.Trace.OpenTracing.IntegrationTests\results
if %errorlevel% neq 0 exit /b %errorlevel%

dotnet test --filter "RunOnWindows=True|Category=Smoke" --configuration %buildConfiguration% -p:Platform=%buildPlatform% .\test\Datadog.Trace.ClrProfiler.IntegrationTests\Datadog.Trace.ClrProfiler.IntegrationTests.csproj --logger trx -r .\test\Datadog.Trace.ClrProfiler.IntegrationTests\results
if %errorlevel% neq 0 exit /b %errorlevel%
