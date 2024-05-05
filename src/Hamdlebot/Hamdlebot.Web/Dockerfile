FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Install Node.js
RUN curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y \
        nodejs \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /src
COPY ["Hamdlebot.Web/Hamdlebot.Web.csproj", "Hamdlebot.Web/"]
RUN dotnet restore "Hamdlebot.Web/Hamdlebot.Web.csproj"
COPY . .
WORKDIR "/src/Hamdlebot.Web"
RUN dotnet build "Hamdlebot.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Hamdlebot.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Hamdlebot.Web.dll"]
