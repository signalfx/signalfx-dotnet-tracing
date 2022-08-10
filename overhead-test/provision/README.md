>ℹ️&nbsp;&nbsp; Provisioning scripts were reused with minimal modifications from [splunk-otel-java-overhead-test](https://github.com/signalfx/splunk-otel-java-overhead-test).

The overhead tests attempt to reduce the impact of noisy neighbors by provisioning
the collector and sqlserver services on a separate cloud instance.

This directory contains automation tooling used to provision and configure 
the EC2 instances used for this test. It uses an internal Splunk tool 
called [Orca](https://core-ee.splunkdev.page/orca/) to do the provisioning 
and ansible to automatically configure the VMs.

# Setup

* You need to be on the corporate VPN.
* Follow the docs and links here [to request access](https://core-ee.splunkdev.page/orca/docs/providers/aws#through-cli).

## Install orca

Orca is a Splunk-internal tool for provisioning cloud instances.
[Go here to learn how to set it up](https://core-ee.splunkdev.page/orca/docs/setup).

## Install ansible

Use `pip` to install [ansible](https://docs.ansible.com/ansible/latest/installation_guide/intro_installation.html#)

## Install jinja2 template engine

See [here](https://github.com/mattrobenolt/jinja2-cli#install) for installation instructions.

# Provisioning

Provisioning will create one instance called "testbox" and one instance called "externals".

Before provisioning, you probably want to reset the token by running:
```
orca config auth
```
and entering your user/pass (you can leave the team blank).

Next, run `provision.sh` script.

If all is successful, your two instances should be set up and ready to use. You
can verify with `orca --cloud aws show containers`

# Run tests

To start the tests on the `TESTBOX_HOST` backgrounded in `screen`, simply 
run the `start-remote-test.sh` script.

You can use `wait-for-test-complete.sh` script to wait until test completes.

# Fetch results

You can fetch results by running `fetch-results.sh` script.

It fetches results and places them in `results` directory