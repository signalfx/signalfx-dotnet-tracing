@ECHO OFF
ECHO\
ECHO Usage:
ECHO     BatchRunComputerAndExceptions.bat [-CleanTestRunDir]
ECHO\

ECHO =========== Test run settings; edit this script to adjust them as needed: ===========
SET SIGNALFX_INTERNAL_DEMORUN_ITERATIONS=100
ECHO SIGNALFX_INTERNAL_DEMORUN_ITERATIONS=%SIGNALFX_INTERNAL_DEMORUN_ITERATIONS%

SET SIGNALFX_INTERNAL_DEMORUN_TIMEOUT_SEC=185
ECHO SIGNALFX_INTERNAL_DEMORUN_TIMEOUT_SEC=%SIGNALFX_INTERNAL_DEMORUN_TIMEOUT_SEC%

ECHO\

SET SIGNALFX_INTERNAL_BIN_OUTPUT_ROOT=%~dp0..\..\..\..\_build\
SET SIGNALFX_INTERNAL_CURRENT_TEST_DIR=%SIGNALFX_INTERNAL_BIN_OUTPUT_ROOT%TestRuns\BatchRunSimpleDemos

IF "%1"=="-CleanTestRunDir" (
    ECHO =========== -CleanTestRunDir specified. Cleaning test directory... ===========
    ECHO SIGNALFX_INTERNAL_CURRENT_TEST_DIR=%SIGNALFX_INTERNAL_CURRENT_TEST_DIR%
    ECHO\
    del %SIGNALFX_INTERNAL_CURRENT_TEST_DIR% /f /s /q
    rmdir %SIGNALFX_INTERNAL_CURRENT_TEST_DIR% /s /q
) ELSE (
    ECHO =========== -CleanTestRunDir NOT specified. Leaving test directory intact. ===========
    ECHO SIGNALFX_INTERNAL_CURRENT_TEST_DIR=%SIGNALFX_INTERNAL_CURRENT_TEST_DIR%
)

ECHO\

ECHO =========== Calling DDProf-SetEnv to configure the basic environment variables... =========== 
call %SIGNALFX_INTERNAL_BIN_OUTPUT_ROOT%DDProf-Deploy\DDProf-SetEnv.bat 

@ECHO OFF
ECHO =========== Completed execution of DDProf-SetEnv. =========== 

ECHO\
ECHO =========== Configuring test-specific directories... ===========
ECHO\

SET SIGNALFX_PROFILING_LOG_DIR=%SIGNALFX_INTERNAL_CURRENT_TEST_DIR%\Logs
SET SIGNALFX_PROFILING_OUTPUT_DIR=%SIGNALFX_INTERNAL_CURRENT_TEST_DIR%\PProf-Files

ECHO SIGNALFX_PROFILING_LOG_DIR=%SIGNALFX_PROFILING_LOG_DIR%
ECHO SIGNALFX_PROFILING_OUTPUT_DIR=%SIGNALFX_PROFILING_OUTPUT_DIR%

SET SIGNALFX_INTERNAL_DEMODIR_COMPUTER=%SIGNALFX_INTERNAL_BIN_OUTPUT_ROOT%bin\Debug-AnyCPU\Demos\Computer01\
SET SIGNALFX_INTERNAL_DEMODIR_EXCEPTIONGENERATOR=%SIGNALFX_INTERNAL_BIN_OUTPUT_ROOT%bin\Debug-AnyCPU\Demos\ExceptionGenerator\

ECHO\
ECHO =========== Running the Demo "Computer" on Net Fx %SIGNALFX_INTERNAL_DEMORUN_ITERATIONS% times for %SIGNALFX_INTERNAL_DEMORUN_TIMEOUT_SEC% seconds each... ===========

for /l %%i in (1, 1, %SIGNALFX_INTERNAL_DEMORUN_ITERATIONS%) do (

  ECHO\
  ECHO =========== Running the Demo "Computer" on Net Fx, iteration %%i of %SIGNALFX_INTERNAL_DEMORUN_ITERATIONS%... ===========
  ECHO\
  %SIGNALFX_INTERNAL_DEMODIR_COMPUTER%net45\Datadog.Demos.Computer01.exe --timeout %SIGNALFX_INTERNAL_DEMORUN_TIMEOUT_SEC%
)

ECHO =========== Completed running the Demo "Computer" on Net Fx %SIGNALFX_INTERNAL_DEMORUN_ITERATIONS% times. ===========
ECHO\

ECHO\
ECHO =========== Running the Demo "Computer" on Net Core %SIGNALFX_INTERNAL_DEMORUN_ITERATIONS% times for %SIGNALFX_INTERNAL_DEMORUN_TIMEOUT_SEC% seconds each... ===========

for /l %%i in (1, 1, %SIGNALFX_INTERNAL_DEMORUN_ITERATIONS%) do (

  ECHO\
  ECHO =========== Running the Demo "Computer" on Net Core, iteration %%i of %SIGNALFX_INTERNAL_DEMORUN_ITERATIONS%... ===========
  ECHO\
  %SIGNALFX_INTERNAL_DEMODIR_COMPUTER%netcoreapp3.1\Datadog.Demos.Computer01.exe --timeout %SIGNALFX_INTERNAL_DEMORUN_TIMEOUT_SEC%
)

ECHO =========== Completed running the Demo "Computer" on Net Core %SIGNALFX_INTERNAL_DEMORUN_ITERATIONS% times. ===========
ECHO\

ECHO\
ECHO =========== Running the Demo "ExceptionGenerator" on Net Fx %SIGNALFX_INTERNAL_DEMORUN_ITERATIONS% times for %SIGNALFX_INTERNAL_DEMORUN_TIMEOUT_SEC% seconds each... ===========

for /l %%i in (1, 1, %SIGNALFX_INTERNAL_DEMORUN_ITERATIONS%) do (

  ECHO\
  ECHO =========== Running the Demo "ExceptionGenerator" on Net Fx, iteration %%i of %SIGNALFX_INTERNAL_DEMORUN_ITERATIONS%... ===========
  ECHO\
  %SIGNALFX_INTERNAL_DEMODIR_EXCEPTIONGENERATOR%net45\Datadog.Demos.ExceptionGenerator.exe --timeout %SIGNALFX_INTERNAL_DEMORUN_TIMEOUT_SEC%
)

ECHO =========== Completed running the Demo "ExceptionGenerator" on Net Fx %SIGNALFX_INTERNAL_DEMORUN_ITERATIONS% times. ===========
ECHO\

ECHO\
ECHO =========== Running the Demo "ExceptionGenerator" on Net Core %SIGNALFX_INTERNAL_DEMORUN_ITERATIONS% times for %SIGNALFX_INTERNAL_DEMORUN_TIMEOUT_SEC% seconds each... ===========

for /l %%i in (1, 1, %SIGNALFX_INTERNAL_DEMORUN_ITERATIONS%) do (

  ECHO\
  ECHO =========== Running the Demo "ExceptionGenerator" on Net Core, iteration %%i of %SIGNALFX_INTERNAL_DEMORUN_ITERATIONS%... ===========
  ECHO\
  %SIGNALFX_INTERNAL_DEMODIR_EXCEPTIONGENERATOR%netcoreapp3.1\Datadog.Demos.ExceptionGenerator.exe --timeout %SIGNALFX_INTERNAL_DEMORUN_TIMEOUT_SEC%
)

ECHO =========== Completed running the Demo "ExceptionGenerator" on Net Core %SIGNALFX_INTERNAL_DEMORUN_ITERATIONS% times. ===========
ECHO\

ECHO\
ECHO ON
