FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5001
EXPOSE 5002

ENV ASPNETCORE_URLS=http://+:5001
ENV ASPNETCORE_HTTP_PORTS=5001

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["./police_poll_service.csproj", "police_poll_service/"]
RUN dotnet restore "police_poll_service/police_poll_service.csproj"
COPY . police_poll_service/
WORKDIR "/src/police_poll_service"
RUN dotnet build "./police_poll_service.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./police_poll_service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "police_poll_service.dll"]