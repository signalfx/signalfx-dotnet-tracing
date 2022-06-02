#!/bin/bash

# initiates a remote test run of 5 passes.

MYDIR=$(dirname $0)

source ${MYDIR}/env.sh

ssh -f -o "LogLevel=ERROR"\
    -i ~/.orca/id_rsa \
    splunk@${TESTBOX_HOST} \
    'screen -dm bash -c "./run-tests.sh 5; bash"'
