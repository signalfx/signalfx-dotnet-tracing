#!/bin/bash

# blocks until the test is complete.

MYDIR=$(dirname $0)
source ${MYDIR}/env.sh

echo "Waiting for tests to complete...zzz..."

function poll_wait(){
  sleep 30
}

while [ 1 == 1 ] ; do
  PASS=$(ssh -o "StrictHostKeyChecking=no" -o "UserKnownHostsFile=/dev/null" -o "LogLevel=ERROR" -i ~/.orca/id_rsa  splunk@${TESTBOX_HOST} "cat /tmp/passnum.txt")
  TOTAL=$(echo $PASS | cut -d ' ' -f 4)
  CUR=$(echo $PASS | cut -d ' ' -f 2)
  echo "Run $CUR of $TOTAL ... zzzzz"
  if [ "$CUR" == "$TOTAL" ] ; then
    break
  fi
  poll_wait
done
sleep 10
echo "We are on the final run."

while [ 1 == 1 ] ; do
  RUNNING=$(ssh -o "StrictHostKeyChecking=no" -o "UserKnownHostsFile=/dev/null" -o "LogLevel=ERROR" -i ~/.orca/id_rsa  splunk@${TESTBOX_HOST} "cat /tmp/tests-running")
  if [ "$RUNNING" == "0" ] ; then
    break
  fi
  poll_wait
done

echo "Test pass is complete."