# Release Process

1. Use nuke target to update version:

    - `nuke UpdateVersion --version {new-version-here}`
    - (for pre-releases)
      `nuke UpdateVersion --version {new-version-here} --is-prerelease true`

2. Update the [CHANGELOG.md](../CHANGELOG.md) with the new release.

3. Create a Pull Request on GitHub with the changes above.

4. Once the Pull Request with all the version changes has been approved and merged
   it is time to perform a [bug bash](https://en.wikipedia.org/wiki/Bug_bash).
   When testing, we should:
   
   - use different testing environments,
   - use different sample applications,
   - test different features,
   - follow the documentation (e.g. do not use the developer's build output,
     but use the installation packages instead). 

5. Create a signed tag for the merged commit.

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

5. Monitor and check the [`Release draft` GitHub workflow](https://github.com/signalfx/signalfx-dotnet-tracing/actions/workflows/release-draft.yml)
   for any errors.

6. Double-check and test the GitHub release artifacts.

7. Update the GitHub release description using the [CHANGELOG.md](../CHANGELOG.md)
   and publish the release.

8. [Publish](https://docs.microsoft.com/en-us/nuget/nuget-org/publish-a-package)
   the NuGet packages to the offical [nuget.org feed](https://www.nuget.org/).
