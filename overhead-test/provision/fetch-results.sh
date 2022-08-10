#!/bin/bash

MYDIR=$(dirname $0)
source ${MYDIR}/env.sh

TS=$(ssh -o "StrictHostKeyChecking=no" -o "UserKnownHostsFile=/dev/null" -o "LogLevel=ERROR" -i ~/.orca/id_rsa splunk@${TESTBOX_HOST} "date -r bin/results/ '+%Y%m%d_%H%M%S'")
echo Timestamp dir will be ${TS}

RESULTS=${MYDIR}/../results/${TS}
mkdir -p $RESULTS

# fetch only csv results and json config
rsync \
  -avv \
  --progress \
  --include="*/" \
  --include="results.csv" \
  --include="config.json" \
  --exclude="*.*" \
  -m \
  -e \
  'ssh -o "StrictHostKeyChecking=no" -o "UserKnownHostsFile=/dev/null" -o "LogLevel=ERROR" -i ~/.orca/id_rsa' \
   "splunk@${TESTBOX_HOST}:bin/results/" "${RESULTS}/"
