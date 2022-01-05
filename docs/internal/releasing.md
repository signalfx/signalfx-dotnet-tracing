# Release Process

## Prepare a release

1. Use nuke target to update version:

    - `nuke UpdateVersion --version {new-version-here}`
    - (for pre-releases)
      `nuke UpdateVersion --version {new-version-here} --is-prerelease true`

2. Update the [CHANGELOG.md](../CHANGELOG.md) with new the new release.
3. Push the changes to upstream and create a Pull Request on GitHub.
4. Once the Pull Request with all the version changes has been approved and merged
    it is time to create a signed for the merged commit.

    ***IMPORTANT***: It is critical you use the same tag
    that you used in the Pre-Release step!
    Failure to do so will leave things in a broken state.

    You can do this using the following Bash snippet.

    ```bash
    TAG='v{new-version-here}'
    COMMIT='{commit-sha-here}'
    git tag -s $TAG $COMMIT
    git push {remote-to-the-main-repo} $TAG
    ```

## Publish the release

After you push the Git tag, a GitHub workflow should start creating a draft release.

1. Monitor and check the [`Release draft` GitHub workflow](https://github.com/signalfx/signalfx-dotnet-tracing/actions/workflows/release-draft.yml)
   for any errors.
2. Double-check and test the release artifacts.
3. Update the release description using the [CHANGELOG.md](../CHANGELOG.md).
4. Publish the GitHub release.
5. [Publish](https://docs.microsoft.com/en-us/nuget/nuget-org/publish-a-package)
   the NuGet packages to the offical [nuget.org feed](https://www.nuget.org/).
