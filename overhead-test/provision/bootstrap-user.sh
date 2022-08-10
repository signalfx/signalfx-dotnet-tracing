#!/bin/bash

# sets up the initial ssh configuration so that subsequent ansible
# operations use your ssh key

MYDIR=$(dirname $0)
ANSIBLE_DIR="${MYDIR}/ansible"

echo "Bootstrapping user"
cat ${MYDIR}/env.sh
source ${MYDIR}/env.sh

jinja2 \
    -D testbox_host=${TESTBOX_HOST} \
    -D externals_host=${EXTERNALS_HOST} \
    -D ansible_user=root \
    ${ANSIBLE_DIR}/hosts.yml.jinja > ${ANSIBLE_DIR}/root.hosts.yml

jinja2 \
    -D testbox_host=${TESTBOX_HOST} \
    -D externals_host=${EXTERNALS_HOST} \
    -D ansible_user=splunk \
    ${ANSIBLE_DIR}/hosts.yml.jinja > ${ANSIBLE_DIR}/hosts.yml

ansible-playbook -i ${ANSIBLE_DIR}/root.hosts.yml ${ANSIBLE_DIR}/bootstrap-user.yml
