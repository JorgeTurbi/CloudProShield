FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["ClOUDSHIELD/CloudShield.csproj", "CloudShield/"]
# COPY ["SharedLibrary/SharedLibrary.csproj", "SharedLibrary/"]
RUN dotnet restore "ClOUDSHIELD/CloudShield.csproj"
COPY . .
WORKDIR "/src/CloudShield"
RUN dotnet build "CloudShield.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CloudShield.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .


ENTRYPOINT ["dotnet", "CloudShield.dll"]