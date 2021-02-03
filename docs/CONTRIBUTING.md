### Preparing release

1. Update the desired version in [`Datadog.Core.Tools.TracerVersion`](../tools/Datadog.Core.Tools/TracerVersion.cs).
2. In build container (`docker-compose run build bash`):
    * `cd /project/tools/PrepareRelease`
    * `dotnet run --project . versions`
3. Submit a PR updating the version with the changes above.
4. Approve and merge PR above.
5. Create the label for the release and add all the CircleCI artifacts from the PR above.
There are artifacts: under `Alpine`, `Linux`, and `Windows`.
6. Publish the release on GH.
7. Publish `SignalFx.NET.Tracing.Azure.Site.Extension.*.nupkg`, `SignalFx.Tracing.*.nupkg`, and `SignalFx.Tracing.OpenTracing.*.nupkg` (from `Windows` artifacts) to https://www.nuget.org/

*Modified by SignalFx*