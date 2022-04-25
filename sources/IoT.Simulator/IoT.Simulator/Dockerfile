FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS buildwithupdatedca
#RUN apt update & apt upgrade
## REQUIRED TO OVERCOME THE ISSUE INTRODUCED AT 2021/01 DUE TO CA CERTIFICATES
#https://github.com/NuGet/Announcements/issues/49
RUN apt-get update
##

FROM buildwithupdatedca AS build
WORKDIR /src
COPY ["IoT.Simulator/IoT.Simulator.csproj", "IoT.Simulator/"]
RUN dotnet restore "IoT.Simulator/IoT.Simulator.csproj"
COPY . .
#RUN rm -f dpssettings.*
WORKDIR "/src/IoT.Simulator"
RUN dotnet build "IoT.Simulator.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "IoT.Simulator.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "IoT.Simulator.dll"]

EXPOSE 8883
EXPOSE 443
EXPOSE 5671

LABEL version="v6.0" environment="dev" author="Jon Mikel Inza" copyright="JMI" github="https://github.com/jonmikeli/azureiotdevicesimulator6-dps" name="IoT.Simulator.Dps" description="IoT Simulator docker image. Implements DPS provisioning with symmetric key and CA X509 authentication." tags="IoT;Simulator;DPS;IoTHub;Azure IoT SDK;net6;csharp;Symmetric key;CA X509"