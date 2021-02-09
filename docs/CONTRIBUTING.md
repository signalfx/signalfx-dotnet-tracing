# Contributing

> Modified by SignalFx

## Development Container

The repository contains configuration for [developing inside a Container](https://code.visualstudio.com/docs/remote/containers) using [Visual Studio Code Remote - Containers extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers).

- [Installation](https://code.visualstudio.com/docs/remote/containers#_installation) 

The Development Container configuration mixes [Docker in Docker](https://github.com/microsoft/vscode-dev-containers/tree/master/containers/docker-in-docker) and [C# (.NET)](https://github.com/microsoft/vscode-dev-containers/tree/master/containers/dotnet) definitions. Use the [Official Development Container Definitions](https://github.com/microsoft/vscode-dev-containers) if more is needed.

There may be a lot of errors, because some projects target .NET Framework. Switch to `Datadog.Trace.Minimal.sln` or `Datadog.Trace.Native.sln` using `F1 -> OmniSharp: Select Project` in Visual Studio Code to load a subset of projects which work without any issues. You can also try building the projects which have errors as it sometimes helps.

If for whatever reason you need to use `Datadog.Trace.sln` you can run `for i in **/*.csproj; do dotnet build $i; done` to decrease the number of errors.

## Releasing

1. Update the desired version in [`Datadog.Core.Tools.TracerVersion`](../tools/Datadog.Core.Tools/TracerVersion.cs).
2. In build container (`docker-compose run build bash`):
    * `cd /project/tools/PrepareRelease`
    * `dotnet run --project . versions`
3. Submit a PR updating the version with the changes above.
4. Approve and merge PR above.
5. Publish a new GitHub Release and describe what has changed. CircleCI will automatically add artifacts and publish a new NuGet package.
