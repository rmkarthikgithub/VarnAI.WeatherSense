# Multi-stage Docker build for VarnAI WeatherSense Web API (.NET 10)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy csproj files and restore dependencies
COPY src/VarnAI.WeatherSense.Domain/*.csproj src/VarnAI.WeatherSense.Domain/
COPY src/VarnAI.WeatherSense.Application/*.csproj src/VarnAI.WeatherSense.Application/
COPY src/VarnAI.WeatherSense.Infrastructure/*.csproj src/VarnAI.WeatherSense.Infrastructure/
COPY src/VarnAI.WeatherSense.API/*.csproj src/VarnAI.WeatherSense.API/

RUN dotnet restore src/VarnAI.WeatherSense.API/VarnAI.WeatherSense.API.csproj

# Copy source code and build
COPY src/ src/
RUN dotnet publish src/VarnAI.WeatherSense.API/VarnAI.WeatherSense.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "VarnAI.WeatherSense.API.dll"]
