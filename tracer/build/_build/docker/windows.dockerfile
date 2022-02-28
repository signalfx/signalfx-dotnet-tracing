# escape=`

#FROM mcr.microsoft.com/dotnet/framework/sdk:4.8-20220215-windowsservercore-ltsc2019
FROM mcr.microsoft.com/dotnet/framework/sdk:3.5-20220215-windowsservercore-ltsc2019
#FROM mcr.microsoft.com/dotnet/framework/runtime:3.5-20220208-windowsservercore-ltsc2019

# ENV COMPLUS_NGenProtectedProcess_FeatureEnabled=0

# RUN `
#     # Install .NET Fx 3.5
#     curl -fSLo microsoft-windows-netfx3.zip https://dotnetbinaries.blob.core.windows.net/dockerassets/microsoft-windows-netfx3-ltsc2019.zip `
#     && tar -zxf microsoft-windows-netfx3.zip `
#     && del /F /Q microsoft-windows-netfx3.zip `
#     && dism /Online /Quiet /Add-Package /PackagePath:.\microsoft-windows-netfx3-ondemand-package~31bf3856ad364e35~amd64~~.cab `
#     && del microsoft-windows-netfx3-ondemand-package~31bf3856ad364e35~amd64~~.cab `
#     && powershell Remove-Item -Force -Recurse ${Env:TEMP}\* `
#     `
#     # Apply latest patch
#     && curl -fSLo patch.msu http://download.windowsupdate.com/c/msdownload/update/software/secu/2022/02/windows10.0-kb5010351-x64_f7ba53f4c410299fc28400f7a21b7f616f635a7c.msu `
#     && mkdir patch `
#     && expand patch.msu patch -F:* `
#     && del /F /Q patch.msu `
#     && dism /Online /Quiet /Add-Package /PackagePath:C:\patch\windows10.0-kb5010351-x64.cab `
#     && rmdir /S /Q patch `
#     `
#     # ngen .NET Fx
#     && %windir%\Microsoft.NET\Framework64\v2.0.50727\ngen uninstall "Microsoft.Tpm.Commands, Version=10.0.0.0, Culture=Neutral, PublicKeyToken=31bf3856ad364e35, processorArchitecture=amd64" `
#     && %windir%\Microsoft.NET\Framework64\v2.0.50727\ngen update `
#     && %windir%\Microsoft.NET\Framework\v2.0.50727\ngen update

ENV chocolateyVersion 0.12.1
RUN powershell -Command "Set-ExecutionPolicy Bypass -Scope Process -Force; [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))"

RUN choco install visualstudio2019buildtools --version 16.11.10.0 --yes
RUN choco install visualstudio2019-workload-nativedesktop --version 1.0.1 --yes

#RUN choco install visualstudio2022professional --version 117.1.0.0 --yes --package-parameters '--add Microsoft.VisualStudio.Workload.NativeDesktop --add Microsoft.VisualStudio.Workload.ManagedDesktop -add Microsoft.VisualStudio.Workload.NetWeb --add Microsoft.Net.Component.4.7.TargetingPack --includeRecommended --includeOptional --passive --locale en-US'

RUN choco install visualstudio2022buildtools --version 117.1.0.0 --yes
RUN choco install visualstudio2022-workload-vctools --version 1.0.0 --yes
RUN choco install visualstudio2022-workload-manageddesktop --version 1.0.1 --yes
RUN choco install visualstudio2022-workload-netweb --version 1.0.0 --yes
RUN choco install wixtoolset --version 3.11.2 --yes

ENV VSTUDIO_ROOT "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools"

COPY . /project
WORKDIR /project
