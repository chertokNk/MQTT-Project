# Этот этап используется при запуске из VS в быстром режиме (по умолчанию для конфигурации отладки)
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
RUN apt-get update && \
    apt-get install -y libldap2-dev && \
    rm -rf /var/lib/apt/lists/*
RUN apt-get update && apt-get install -y telnet && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app

# Этот этап используется для сборки проекта службы
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["MQTT.Server/MQTT.Server.csproj", "MQTT.Server/"]
RUN dotnet restore "MQTT.Server/MQTT.Server.csproj"
COPY . .
WORKDIR "/src/MQTT.Server"
RUN dotnet build "MQTT.Server.csproj" -c Release -o /app/build

# Этот этап используется для публикации проекта службы, который будет скопирован на последний этап
FROM build AS publish
RUN dotnet publish "MQTT.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Этот этап используется в рабочей среде или при запуске из VS в обычном режиме (по умолчанию, когда конфигурация отладки не используется)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
RUN apt-get update && \
    apt-get install -y libldap2-dev && \
    rm -rf /var/lib/apt/lists/*
RUN apt-get update && apt-get install -y telnet && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 23

ENTRYPOINT ["dotnet", "MQTT.Server.dll"]