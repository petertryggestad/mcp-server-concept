FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src
COPY WeatherWeb/WeatherWeb.csproj WeatherWeb/
RUN dotnet restore "WeatherWeb/WeatherWeb.csproj"
COPY WeatherWeb/ WeatherWeb/
RUN dotnet publish "WeatherWeb/WeatherWeb.csproj" -c Release -o /app/publish --runtime linux-x64 --self-contained false

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "WeatherWeb.dll"]
