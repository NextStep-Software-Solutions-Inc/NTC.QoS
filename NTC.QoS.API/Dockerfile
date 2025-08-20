# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG ASPNETCORE_ENVIRONMENT=Production

WORKDIR /src
COPY ["NTC.QoS.API/NTC.QoS.API.csproj", "NTC.QoS.API/"]
COPY ["NTC.QoS.Application/NTC.QoS.Application.csproj", "NTC.QoS.Application/"]
COPY ["NTC.QoS.Domain/NTC.QoS.Domain.csproj", "NTC.QoS.Domain/"]
COPY ["NTC.QoS.Infrastructure/NTC.QoS.Infrastructure.csproj", "NTC.QoS.Infrastructure/"]
RUN dotnet restore "./NTC.QoS.API/NTC.QoS.API.csproj"
COPY . .
WORKDIR "/src/NTC.QoS.API"
RUN dotnet build "./NTC.QoS.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
ARG ASPNETCORE_ENVIRONMENT=Production
RUN dotnet publish "./NTC.QoS.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ARG ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_ENVIRONMENT=$ASPNETCORE_ENVIRONMENT
ENTRYPOINT ["dotnet", "NTC.QoS.API.dll"]