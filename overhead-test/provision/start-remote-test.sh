#!/bin/bash

# initiates a remote test run of 5 passes.

MYDIR=$(dirname $0)

source ${MYDIR}/env.sh

ssh -f -o "LogLevel=ERROR" \
    -o "StrictHostKeyChecking=no" \
    -o "UserKnownHostsFile=/dev/null"\
    -i ~/.orca/id_rsa \
    splunk@${TESTBOX_HOST} \
    'screen -dm bash -c "./run-tests.sh 3; bash"'
