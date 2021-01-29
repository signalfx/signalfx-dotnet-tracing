FROM ubuntu:18.04

RUN apt-get update && apt-get install -y \
    build-essential \
    git \
    rpm \
    ruby \
    ruby-dev \
    rubygems

RUN gem install --no-ri --no-rdoc fpm
