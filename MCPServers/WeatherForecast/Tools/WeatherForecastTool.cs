using System.ComponentModel;
using ModelContextProtocol.Server;
using WeatherForecast.Services;

namespace WeatherForecast;

[McpServerToolType]
public class WeatherForecastTool
{
    private readonly WeatherForecastService _service;

    public WeatherForecastTool(WeatherForecastService service)
    {
        _service = service;
    }

    [McpServerTool, Description("Henter nåværende vær for en geografisk posisjon fra Meteorologisk institutts API (api.met.no / Yr). Returnerer temperatur, vind, luftfuktighet og værtype.")]
    public async Task<string> GetCurrentWeather(
        [Description("Breddegrad (latitude) for lokasjonen, f.eks. 59.9139 for Oslo")] double latitude,
        [Description("Lengdegrad (longitude) for lokasjonen, f.eks. 10.7522 for Oslo")] double longitude)
    {
        return await _service.GetCurrentWeatherAsync(latitude, longitude);
    }

    [McpServerTool, Description("Henter værvarsel for de neste timene for en geografisk posisjon fra Meteorologisk institutts API (api.met.no / Yr).")]
    public async Task<string> GetWeatherForecast(
        [Description("Breddegrad (latitude) for lokasjonen, f.eks. 59.9139 for Oslo")] double latitude,
        [Description("Lengdegrad (longitude) for lokasjonen, f.eks. 10.7522 for Oslo")] double longitude,
        [Description("Antall timer fremover å hente varsel for (1–72, standard er 24)")] int hours = 24)
    {
        return await _service.GetForecastAsync(latitude, longitude, hours);
    }
}
