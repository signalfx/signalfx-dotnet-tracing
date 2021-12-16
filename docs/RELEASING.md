# Releasing

1. Use nuke target to update version: 
    * `nuke UpdateVersion --version {new-version-here}`
    * `nuke UpdateVersion --version {new-version-here} --is-prerelease true` (for pre-releases)
2. Submit a PR updating the version with the changes above.
3. Approve and merge PR above.
4. Publish a new GitHub Release and describe what has changed. Github Actions will automatically add artifacts and publish a new NuGet package.