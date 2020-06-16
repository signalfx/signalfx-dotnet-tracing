@rem Simple Windows cmd file to build samples-aspnet files used in integration tests for AspNet.
setlocal enabledelayedexpansion

set buildConfiguration=Debug
set buildPlatform=x64

@rem publishOutput MUST be the an absolute path since projects in sub-directories will use it.
set publishOutput=%~dp0src\bin\managed-publish

@rem samples-aspnet require files from VS install to build, use the VS batch to set proper env vars
@rem so this script must be run from a VS command shell. 
call VsDevCmd.bat
if %errorlevel% neq 0 exit /b %errorlevel%
@echo on

for /f %%i in ('dir /s/b .\samples-aspnet\*.csproj') do  (
    msbuild -p:Configuration=%buildConfiguration% -p:Platform=%buildPlatform% -p:ManagedProfilerOutputDirectory=%publishOutput%  -p:LoadManagedProfilerFromProfilerDirectory=true %%i
    if !errorlevel! neq 0 exit /b !errorlevel!
    @echo on
)
