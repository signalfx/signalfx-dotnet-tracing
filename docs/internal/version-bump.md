# Dependency bumping

This section is tracking dependencies that are needed to be bumped from time to time.

| Dependency | Files | Bumping | Notes |
|-|-|-|-|
| Nuget | .csproj | Upstream | Test packages might need to stay in certain version. |
| Github CI | ./github/workflows/*.yml | Dependabot | Bumps Github step templates |
| Docker | *.dockerfile | Dependabot | Bumps Docker image versions |
| dotnet SDK | (CI templates) | Manual | Search for ```actions/setup-dotnet``` or ```dotnetSdkVersion:``` |
| ASP.NET Runtime | *.dockerfile | Manual | Search for ```./dotnet-install.sh --runtime aspnetcore``` |
| Gihub CI OS | ./github/workflows/*.yml | Manual | Search for ```runs-on:``` |
| APT | debian.dockerfile | Manual | Search for ```apt-get install``` |
| Ruby gems | *.dockerfile | Manual | Search for ```gem install``` |

