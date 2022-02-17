@ECHO OFF
ECHO\
ECHO -- Activating Datadog .NET Application Monitoring --
ECHO\
ECHO This will only work if you previously installed the following components:
ECHO   * MSI Install Package with .NET Tracer and .NET Profiler Beta
ECHO   * Datadog Agent
ECHO\
ECHO Setting Environment Variables...

SET COR_ENABLE_PROFILING=1
SET COR_PROFILER={0F171A24-3497-4B05-AE6D-B6B313FBF83B}

SET CORECLR_ENABLE_PROFILING=1
SET CORECLR_PROFILER={0F171A24-3497-4B05-AE6D-B6B313FBF83B}

ECHO Done.
ECHO\
ECHO Listing all relevant Environment Variables:

ECHO\
SET COR_
SET CORECLR_

ECHO\
SET SIGNALFX_

ECHO\
ECHO You can now run a .NET application and view your Profiles and Traces in the Datadog UI.

ECHO\
ECHO ON