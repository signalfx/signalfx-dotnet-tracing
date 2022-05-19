#!/usr/bin/env bash
set -euox pipefail

# test configurations, specified in the following format:
# configuration short name (used to create results directory),dockerfile target to use to build the app, value of SIGNALFX_PROFILER_ENABLED flag
configurations=("app","baseline-app","0" "instrumented-app","instrumented-app","0" "profiled-app","instrumented-app","1")

for config in ${configurations[*]}; do
  # unpack configuration
  IFS=',' read config_name target profiler_enabled <<< "${config}"
  
  # create directory for test results 
  results_dir=./results/$config_name
  mkdir -p $results_dir
  
  # start dependencies and an app
  app_name=$target profiler_enabled=$profiler_enabled docker-compose up --build StartDependencies
  
  # run warmup (similar to load test, but with limited iterations/users, not exporting the results)
  docker-compose run RunWarmup
  
  # run test
  results_dir=$results_dir docker-compose run RunTest
  
  # cleanup
  docker-compose down
done