@rem Simple Windows cmd file to run equivalent of unit tests
setlocal enabledelayedexpansion

set buildConfiguration=Debug

for %%c in (restore build) do (
    for /f %%i in ('dir /s/b .\src\*.csproj') do  (
        dotnet %%c %%i
        if !errorlevel! neq 0 exit /b !errorlevel!
    )

    for /f %%i in ('dir /s/b .\test\*.Tests.csproj') do  (
        dotnet %%c %%i
        if !errorlevel! neq 0 exit /b !errorlevel!
    )
)

for /f %%i in ('dir /s/b .\test\*.Tests.csproj') do  (
    dotnet test %%i
    if !errorlevel! neq 0 exit /b !errorlevel!
)
