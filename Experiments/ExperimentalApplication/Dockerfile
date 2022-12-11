FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Experiments/ExperimentalApplication/ExperimentalApplication.csproj", "Experiments/ExperimentalApplication/"]
COPY ["RecurrentWorkerService.Distributed.EtcdPersistence/RecurrentWorkerService.Distributed.EtcdPersistence.csproj", "RecurrentWorkerService.Distributed.EtcdPersistence/"]
COPY ["RecurrentWorkerService.Distributed/RecurrentWorkerService.Distributed.csproj", "RecurrentWorkerService.Distributed/"]
COPY ["RecurrentWorkerService.Distributed.Interfaces/RecurrentWorkerService.Distributed.Interfaces.csproj", "RecurrentWorkerService.Distributed.Interfaces/"]
COPY ["RecurrentWorkerService/RecurrentWorkerService.csproj", "RecurrentWorkerService/"]
COPY ["RecurrentWorkerService.Distributed.Prioritization/RecurrentWorkerService.Distributed.Prioritization.csproj", "RecurrentWorkerService.Distributed.Prioritization/"]
COPY ["Experiments/OpenTelemetry/OpenTelemetry.Exporter.InfluxLineProtocolFile.csproj", "Experiments/OpenTelemetry/"]
COPY ["Experiments/InfluxLineProtocol/InfluxLineProtocol.csproj", "Experiments/InfluxLineProtocol/"]
RUN dotnet restore "Experiments/ExperimentalApplication/ExperimentalApplication.csproj"
COPY . .
WORKDIR "/src/Experiments/ExperimentalApplication"
RUN dotnet build "ExperimentalApplication.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ExperimentalApplication.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ExperimentalApplication.dll"]
