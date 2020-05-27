### Preparing release

1. Update the desired version in `Datadog.Core.Tools.TracerVersion`.
2. In build container (`docker-compose run build bash`):
    * `cd /project/tools/PrepareRelease`
    * `dotnet run --project . versions`
    * `dotnet run --project . integrations`
3. Update out of container as needed to avoid undesired changes:
    * `git checkout -- src/Datadog.Trace.ClrProfiler.Managed.Core/AssemblyInfo.cs src/Datadog.Trace.AspNet/AssemblyInfo.cs`
4. Once version PR is appropriately squashed and merged use artifacts for GH release and any built locally as necessary for release:
    * `docker-compose run build`
    * `docker-compose run Profiler`
    * `docker-compose run package`
    * `docker-compose run Profiler.Alpine`
    * `docker-compose run package.alpine`

*Modified by SignalFx*