# Release Process

## Prepare a release

1. Use nuke target to update version:

    - `nuke UpdateVersion --version {new-version-here}`
    - (for pre-releases)
      `nuke UpdateVersion --version {new-version-here} --is-prerelease true`

2. Update the [CHANGELOG.md](CHANGELOG.md) with new the new release.
3. Push the changes to upstream and create a Pull Request on GitHub.

## Tag

Once the Pull Request with all the version changes has been approved and merged
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

## Publish the GitHub release

After you push the Git tag, a GitHub Action should create a draft pre-release
containing artifacts.

## Publish the NuGet packages

Publish the released NuGet packages to the offical [nuget.org feed]<https://www.nuget.org/>:

Instructions: [here](https://docs.microsoft.com/en-us/nuget/nuget-org/publish-a-package)
