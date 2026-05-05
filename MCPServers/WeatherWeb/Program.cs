using WeatherWeb.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

builder.Services.AddHttpClient("default", client =>
    client.DefaultRequestHeaders.UserAgent.ParseAdd("WeatherWebApp/1.0 (github.com/petertryggestad/V-rdata-med-MCP-server)"));

builder.Services.AddScoped<WeatherService>();
builder.Services.AddScoped<GeocodingService>();

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
