# this Makefile is for testing purposes. to be removed later

all: build run

prepare: # run it before testing
	rm .dockerignore

build:
	docker build --file "./tracer/build/_build/docker/windows.dockerfile" --tag win-build .

run:
	docker run --rm --name release win-build powershell -Command ".\tracer\build.ps1 Clean BuildTracerHome PackageTracerHome"
