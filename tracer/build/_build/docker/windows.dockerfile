# escape=`

FROM mcr.microsoft.com/dotnet/framework/sdk:4.8-windowsservercore-ltsc2019

# it would be good to not use choco. however it can be used to create "image templates" or for sake of POC
# reference: https://github.com/DataDog/datadog-agent-buildimages/pull/91

# install choco
ENV chocolateyVersion 0.12.1
RUN Set-ExecutionPolicy Bypass -Scope Process -Force; [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))

# install Visual Studio with needed workloads
RUN choco install visualstudio2022professional --version 117.1.0.0 --yes --params "'--add Microsoft.VisualStudio.Workload.ManagedDesktop --add Microsoft.VisualStudio.Workload.NativeDesktop --add Microsoft.VisualStudio.ComponentGroup.VC.Tools.142.x86.x64 --includeRecommended'"

# workaround for MSBuild auto-detection to not detect 'BuildTools' instead of 'Professional'
RUN Remove-Item -Recurse 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools'

# install WiX Toolset
RUN choco install wixtoolset --version 3.11.2 --yes

# copy the repository
COPY . /project
WORKDIR /project
