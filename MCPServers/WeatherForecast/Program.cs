using WeatherForecast;
using WeatherForecast.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient());
builder.Services.AddScoped<WeatherForecastService>();

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<WeatherForecastTool>();

var app = builder.Build();

app.MapMcp();

app.MapGet("/weather/current", async (double lat, double lon, WeatherForecastService svc) =>
    Results.Ok(await svc.GetCurrentWeatherAsync(lat, lon)));

app.MapGet("/weather/forecast", async (double lat, double lon, int hours, WeatherForecastService svc) =>
    Results.Ok(await svc.GetForecastAsync(lat, lon, hours)));

var serverUrl = builder.Configuration["ServerUrl"] ?? "http://0.0.0.0:4547";
app.Run(serverUrl);
