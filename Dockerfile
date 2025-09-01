FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80
# (opcional, pero recomendable para que el código lea la ruta desde configuración)
ENV Mail__TemplatesPath=/app/Mail/Templates

# Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["CloudShield.csproj", "./"]
RUN dotnet restore "CloudShield.csproj"
COPY . .
RUN dotnet build "CloudShield.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "CloudShield.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# 👇 Asegura que exista la ruta y copia las plantillas desde el código fuente
RUN mkdir -p /app/Mail/Templates
COPY --from=build /src/Mail/Templates /app/Mail/Templates
# (si tienes recursos extra)
# COPY --from=build /src/Mail/Assets /app/Mail/Assets

ENTRYPOINT ["dotnet", "CloudShield.dll"]
