# Releasing

1. Use nuke target to update version: 
    * `nuke UpdateVersion --version {new-version-here}`
    * `nuke UpdateVersion --version {new-version-here} --is-prerelease true` (for pre-releases)
2. Update [CHANGELOG.md](CHANGELOG.md)
3. Submit a PR updating the version with the changes above.
4. Approve and merge PR above.
5. Publish a new GitHub Release with a new version tag and describe what has changed. Github Actions will automatically add artifacts.