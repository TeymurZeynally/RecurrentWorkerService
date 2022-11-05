FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Experiments/EtcdEventsCollector/EtcdEventsCollector.csproj", "Experiments/EtcdEventsCollector/"]
COPY ["RecurrentWorkerService.Distributed.EtcdPersistence/RecurrentWorkerService.Distributed.EtcdPersistence.csproj", "RecurrentWorkerService.Distributed.EtcdPersistence/"]
COPY ["RecurrentWorkerService.Distributed/RecurrentWorkerService.Distributed.csproj", "RecurrentWorkerService.Distributed/"]
COPY ["RecurrentWorkerService.Distributed.Interfaces/RecurrentWorkerService.Distributed.Interfaces.csproj", "RecurrentWorkerService.Distributed.Interfaces/"]
COPY ["RecurrentWorkerService/RecurrentWorkerService.csproj", "RecurrentWorkerService/"]
RUN dotnet restore "Experiments/EtcdEventsCollector/EtcdEventsCollector.csproj"
COPY . .
WORKDIR "/src/Experiments/EtcdEventsCollector"
RUN dotnet build "EtcdEventsCollector.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EtcdEventsCollector.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EtcdEventsCollector.dll"]
