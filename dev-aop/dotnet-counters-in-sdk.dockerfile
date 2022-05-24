FROM mcr.microsoft.com/dotnet/sdk:3.1

RUN dotnet tool install dotnet-counters --tool-path /usr/bin
