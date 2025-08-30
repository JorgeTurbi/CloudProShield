FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar archivo .csproj al directorio actual
COPY ["CloudShield.csproj", "./"]
# Si tienes SharedLibrary, descomenta:
# COPY ["SharedLibrary/SharedLibrary.csproj", "SharedLibrary/"]

# Restaurar desde la ubicación correcta (sin "/" al inicio)
RUN dotnet restore "CloudShield.csproj"

# Copiar todo el código fuente
COPY . .

# No necesitamos cambiar de directorio, ya estamos en /src
RUN dotnet build "CloudShield.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CloudShield.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "CloudShield.dll"]