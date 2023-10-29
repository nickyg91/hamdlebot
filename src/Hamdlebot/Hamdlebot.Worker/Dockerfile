FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Hamdlebot.Worker/Hamdlebot.Worker.csproj", "Hamdlebot.Worker/"]
RUN dotnet restore "Hamdlebot.Worker/Hamdlebot.Worker.csproj"
COPY . .
WORKDIR "/src/Hamdlebot.Worker"
RUN dotnet build "Hamdlebot.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Hamdlebot.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Hamdlebot.Worker.dll"]
