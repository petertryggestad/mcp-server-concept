using WeatherForecast;
using WeatherForecast.Services;

var builder = Host.CreateApplicationBuilder(args);

// stdout brukes av MCP-protokollen — logg kun til stderr
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.AddHttpClient();
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient());
builder.Services.AddScoped<WeatherForecastService>();

builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<WeatherForecastTool>();

await builder.Build().RunAsync();
