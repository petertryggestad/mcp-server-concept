# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy Shared project files for restore
COPY Shared/Shared.csproj Shared/

# Copy project file for restore
COPY WeatherForecast/WeatherForecast.csproj WeatherForecast/

# Restore dependencies (cached layer)
RUN dotnet restore "WeatherForecast/WeatherForecast.csproj"

# Copy remaining source files
COPY Shared/ Shared/
COPY WeatherForecast/ WeatherForecast/

# Build and publish
RUN dotnet publish "WeatherForecast/WeatherForecast.csproj" -c Release -o /app/publish --runtime linux-x64 --self-contained false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY WeatherForecast/appsettings.json ./
EXPOSE 4547
ENTRYPOINT ["dotnet", "WeatherForecast.dll"]
