@echo off

echo Starting install

SET extensionBaseDir=%~dp0
SET siteHome=%HOME%
echo Extension directory is %extensionBaseDir%
echo Site root directory is %siteHome%

REM Create version specific tracer directory
SET tracerDir=%siteHome%\signalfx\tracing\v0.2.10
if not exist %tracerDir% mkdir %tracerDir%

REM Copy tracer to version specific directory
ROBOCOPY %extensionBaseDir%tracer %tracerDir% /E /purge

echo Finished install