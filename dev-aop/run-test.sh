#!/usr/bin/env bash
set -euox pipefail

for profile in "eshop-app" "eshop-app-instrumented"; do
  #TODO: cleanup profile usage
  
  mkdir -p ./results/$profile
  
  # start dependencies and an app from specified profile
  app_name=$profile docker-compose --profile $profile up StartDependencies
  
  # run warmup (similar to load test, but with limited iterations/users, not exporting the results)
  app_name=$profile docker-compose run RunWarmup
  
  # run test
  app_name=$profile docker-compose run RunTest
  
  # cleanup
  app_name=$profile docker-compose --profile $profile down
done