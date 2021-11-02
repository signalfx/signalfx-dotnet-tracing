# Releasing

<!-- TODO update this section when working on generating the deliverables story -->

1. Update the desired version in [`Datadog.Core.Tools.TracerVersion`](../tools/Datadog.Core.Tools/TracerVersion.cs).
2. In build container (`docker-compose run build bash`):
    * `cd /project/tools/PrepareRelease`
    * `dotnet run --project . versions`
3. Submit a PR updating the version with the changes above.
4. Approve and merge PR above.
5. Publish a new GitHub Release and describe what has changed. CircleCI will automatically add artifacts and publish a new NuGet package.
