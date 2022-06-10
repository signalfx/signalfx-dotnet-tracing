#!/bin/bash

MYDIR=$(dirname $0)
source ${MYDIR}/env.sh

TS=$(ssh -o "LogLevel=ERROR" -i ~/.orca/id_rsa splunk@${TESTBOX_HOST} "date -r bin/results/ '+%Y%m%d_%H%M%S'")
echo Timestamp dir will be ${TS}

RESULTS=${MYDIR}/../testresults/${TS}
mkdir -p $RESULTS

# fetches container logs and k6/counters results
rsync -avv --progress -e \
  'ssh -o "LogLevel=ERROR" -i ~/.orca/id_rsa' \
   "splunk@${TESTBOX_HOST}:bin/results/" "${RESULTS}/"

# fetches trx test results
rsync -avv --progress -e \
  'ssh -o "LogLevel=ERROR" -i ~/.orca/id_rsa' \
   "splunk@${TESTBOX_HOST}:TestResults/" "${RESULTS}/"
