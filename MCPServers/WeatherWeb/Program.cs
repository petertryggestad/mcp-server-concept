using WeatherWeb.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

builder.Services.AddHttpClient("default", client =>
    client.DefaultRequestHeaders.UserAgent.ParseAdd("WeatherWebApp/1.0 (github.com/petertryggestad/V-rdata-med-MCP-server)"));

builder.Services.AddScoped<WeatherService>();
builder.Services.AddScoped<GeocodingService>();
builder.Services.AddScoped<ClaudeService>();
builder.Services.AddSingleton(sp =>
{
    var url = sp.GetRequiredService<IConfiguration>()["WeatherForecast:BaseUrl"]
        ?? throw new InvalidOperationException("WeatherForecast:BaseUrl ikke konfigurert");
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return new WeatherWeb.Services.McpWeatherClient(factory, url);
});

builder.Services.AddAuthentication("Cookies")
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.Run();
