FROM python:3.13.1-windowsservercore-1809

WORKDIR /

RUN pip install --no-cache-dir ddapm-test-agent

ENTRYPOINT [ "ddapm-test-agent", "--port=8126" ]