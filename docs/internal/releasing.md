# Release Process

1. Verify if [wizard](https://app.signalfx.com/#/integrations/dotnet-tracing/description)
needs any adjustments. Contact @signalfx/gdi-docs team if needed.

1. Use nuke target to update version:

    - `nuke UpdateVersion --NewVersion {new-version-here} --NewIsPrerelease false`
    - (for pre-releases)
      `nuke UpdateVersion --NewVersion {new-version-here} --NewIsPrerelease true`

1. Update the [CHANGELOG.md](../CHANGELOG.md) with the new release.

1. Create a Pull Request on GitHub with the changes above.

1. Once the Pull Request with all the version changes has been approved and merged
   it is time to create a signed tag for the merged commit.

   ***IMPORTANT***: It is critical you use the same tag
   that you used in the previous steps!
   Failure to do so will leave things in a broken state.

   You can do this using the following Bash snippet.

   ```bash
   TAG='v{new-version-here}'
   COMMIT='{commit-sha-here}'
   git tag -s -m $TAG $TAG $COMMIT
   git push {remote-to-the-main-repo} $TAG
   ```

   After you push the Git tag, a GitHub workflow should start creating a draft release.

1. Monitor and check the [`Release draft` GitHub workflow](https://github.com/signalfx/signalfx-dotnet-tracing/actions/workflows/release-draft.yml)
   for any errors.

1. Double-check and test the GitHub release artifacts.

1. Update the GitHub release description using the [CHANGELOG.md](../CHANGELOG.md)
   and publish the release.

1. [Publish](https://docs.microsoft.com/en-us/nuget/nuget-org/publish-a-package)
   the NuGet packages to the official [nuget.org feed](https://www.nuget.org/).

1. Update `TRACER_VERSION` in [examples repository](https://github.com/signalfx/tracing-examples/blob/main/signalfx-tracing/signalfx-dotnet-tracing/aspnetcore-and-mongodb/InstrumentContainer/Dockerfile)
