# How to perform pull from upstream repo

1. Fetch upstream and origin, be sure to update main branch and create a new branch, let's say pull-upstream-changes
2. Find the range of commits from upstream:
    * Look at previous catchup PR comment and record the sha of the last commit on upstream, let's call it <fst_sha>
    * Look at the last commit on upstream, eg.: git log --oneline upstream/master, let's call it <last_sha>
3. If needed to squash to pass CLA check:
    * Add an empty commit and save its sha (squash_sha): `git commit --allow-empty -m "Upstream sync <fst_sha>..<last_sha>"`
4. `git cherry-pick fst_sha..last_sha`
    * Resolve each conflict, suggestion: run `dotnet build` for the affected projects to confirm that the resolution is good.
    * Run `git cherry-pick --continue` every time that a conflict is resolved
5. Build and fix any integration issues:
    * New usages of env vars, reg ex: ^[^#].*[^A-Z]DD_
    * Upstream CLSID_CorProfiler: `846F5F1C-F9AE-4B07-969E-05C26BC060D8` (happens in launch.settings for new apps) use `B4C89B0F-9908-4F73-9F59-0D77C5A06874` instead.
    * Upstream CLSID_New_CorProfiler: `50DA5EED-F1ED-B00B-1055-5AFE55A1ADE5` use `0F171A24-3497-4B05-AE6D-B6B313FBF83B` instead.
    * Old PublicKeyToken: `def86d061d0d2eeb` use `e43a27c2023d388a` instead.
    * Old log path: `/var/log/datadog` use `/var/log/signalfx` instead.
    * Run unit tests, commit any needed fixes, repeat until passing unit tests
    * Update version in `tracer\src\Datadog.Trace.ClrProfiler.Native\version.h` if the upstream bumped it.
    * Update versions and integrations json by running: `nuke UpdateIntegrationsJson` (remember to revert wcf and other windows-only frameworks if you are using different platform)
    * Run build via nuke and commit any needed fixes, until it passes:

6. If squashing cherry-pick from upstream to pass CLA check:
    * `git rebase -i <squash_sha>^`
    * Select top one as "pick" all coming from upstream as "squash" and let the ones that you made to fix build and test as "pick" so it is easier to review them separately.

## Regenerating snapshot files

Windows in Git Bash:

```sh
git clean -fXd ; ./tracer/build.cmd ; \
Verify_DisableClipboard=true DiffEngine_Disabled=true ./tracer/build.cmd BuildAndRunWindowsIntegrationTests --framework net5.0 ; \
Verify_DisableClipboard=true DiffEngine_Disabled=true ./tracer/build.cmd BuildAndRunWindowsIntegrationTests --framework netcoreapp3.1 ; \
Verify_DisableClipboard=true DiffEngine_Disabled=true ./tracer/build.cmd BuildAndRunWindowsIntegrationTests --framework net461 ; \
Verify_DisableClipboard=true DiffEngine_Disabled=true ./tracer/build.cmd BuildAndRunWindowsIntegrationTests --framework net452 ; \
./tracer/build.cmd OverwriteSnaphotFiles
```

Windows in Ubuntu WSL:

```sh
git clean -fXd ; ./tracer/build.sh ; \
docker-compose run --rm StartDependencies ; \
Verify_DisableClipboard=true DiffEngine_Disabled=true ./tracer/build.sh BuildAndRunLinuxIntegrationTests --framework net5.0 ; \
Verify_DisableClipboard=true DiffEngine_Disabled=true ./tracer/build.sh BuildAndRunLinuxIntegrationTests --framework netcoreapp3.1 ; \
docker-compose down ; \
./tracer/build.sh OverwriteSnaphotFiles
```
