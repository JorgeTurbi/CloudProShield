FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80   

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["CloudShield.csproj", "./"]
RUN dotnet restore "CloudShield.csproj"
COPY . .
RUN dotnet build "CloudShield.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CloudShield.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CloudShield.dll"]
