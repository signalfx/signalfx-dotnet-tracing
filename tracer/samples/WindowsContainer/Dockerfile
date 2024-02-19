#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

#Depending on the operating system of the host machines(s) that will build or run the containers, the image specified in the FROM statement may need to be changed.
#For more information, please see https://aka.ms/containercompat

FROM mcr.microsoft.com/dotnet/aspnet:5.0-windowsservercore-ltsc2019 AS base
WORKDIR /app

ARG TRACER_VERSION=1.1.1
ENV SIGNALFX_TRACER_VERSION=$TRACER_VERSION
ENV ASPNETCORE_URLS=http://*.80

ENV COR_ENABLE_PROFILING="1"
ENV COR_PROFILER="{B4C89B0F-9908-4F73-9F59-0D77C5A06874}"

ENV CORECLR_ENABLE_PROFILING="1"
ENV CORECLR_PROFILER=$COR_PROFILER

# We recommend always using the latest release and regularly updating: https://github.com/signalfx/signalfx-dotnet-tracing/releases/latest
SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

RUN Write-Host "Downloading SignalFx .NET Tracer v$env:SIGNALFX_TRACER_VERSION" ;\
    (New-Object System.Net.WebClient).DownloadFile('https://github.com/signalfx/signalfx-dotnet-tracing/releases/download/v' + $env:SIGNALFX_TRACER_VERSION + '/signalfx-dotnet-tracing-' + $env:SIGNALFX_TRACER_VERSION + '-x64.msi', 'signalfx-dotnet-tracing.msi') ;\
    Write-Host 'Installing SignalFX .NET Tracer' ;\
    Start-Process -Wait msiexec -ArgumentList '/i signalfx-dotnet-tracing.msi /quiet /qn /norestart /log signalfx-dotnet-tracing-msi-installer.log' ; \
    Write-Host 'SignalFx .NET Tracer installed, removing installer file' ; \
	Remove-Item 'signalfx-dotnet-tracing.msi' ;

FROM mcr.microsoft.com/dotnet/sdk:5.0-windowsservercore-ltsc2019 AS build
WORKDIR /src
COPY ["Dotnet.WindowsContainer.Example.csproj", "."]
RUN dotnet restore "./Dotnet.WindowsContainer.Example.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet publish "Dotnet.WindowsContainer.Example.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Dotnet.WindowsContainer.Example.dll"]
