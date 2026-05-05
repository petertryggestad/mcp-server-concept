using System.Globalization;
using System.Text.Json;
using MCPServers.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WeatherForecast.Services;

public class WeatherForecastService : BaseHttpService
{
    private const string BaseUrl = "https://api.met.no/weatherapi/locationforecast/2.0";
    private const string UserAgent = "WeatherForecastMcpServer/1.0 (github.com/atea/mcp-server-concept)";

    public WeatherForecastService(
        IConfiguration configuration,
        HttpClient client,
        ILogger<WeatherForecastService> logger)
        : base(configuration, client, logger)
    {
        _client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
    }

    public async Task<string> GetCurrentWeatherAsync(double latitude, double longitude)
    {
        var url = $"{BaseUrl}/compact?lat={latitude.ToString("F4", CultureInfo.InvariantCulture)}&lon={longitude.ToString("F4", CultureInfo.InvariantCulture)}";
        var json = await GetAsync(url);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var timeseries = root
            .GetProperty("properties")
            .GetProperty("timeseries");

        if (timeseries.GetArrayLength() == 0)
            return "Ingen værdata tilgjengelig for denne lokasjonen.";

        var current = timeseries[0];
        var time = current.GetProperty("time").GetString();
        var instant = current
            .GetProperty("data")
            .GetProperty("instant")
            .GetProperty("details");

        var next1h = current.GetProperty("data").TryGetProperty("next_1_hours", out var n1h) ? n1h : (JsonElement?)null;

        var temp = instant.GetProperty("air_temperature").GetDouble();
        var wind = instant.GetProperty("wind_speed").GetDouble();
        var windDir = instant.GetProperty("wind_from_direction").GetDouble();
        var humidity = instant.GetProperty("relative_humidity").GetDouble();
        var precip = instant.TryGetProperty("precipitation_rate", out var pr) ? pr.GetDouble() : (double?)null;

        string? symbolCode = null;
        double? precipAmount = null;
        if (next1h.HasValue)
        {
            next1h.Value.GetProperty("summary").TryGetProperty("symbol_code", out var sym);
            symbolCode = sym.ValueKind != JsonValueKind.Undefined ? sym.GetString() : null;
            if (next1h.Value.GetProperty("details").TryGetProperty("precipitation_amount", out var pa))
                precipAmount = pa.GetDouble();
        }

        var windDescription = WindDirectionToNorwegian(windDir);
        var weatherDescription = symbolCode != null ? SymbolToNorwegian(symbolCode) : "Ukjent";

        var result = $"""
            Vær for koordinater ({latitude:F4}, {longitude:F4})
            Tidspunkt: {time}

            Temperatur: {temp:F1} °C
            Vind: {wind:F1} m/s fra {windDescription} ({windDir:F0}°)
            Luftfuktighet: {humidity:F0} %
            Værtype (neste time): {weatherDescription}
            """;

        if (precipAmount.HasValue)
            result += $"\nNedbør neste time: {precipAmount:F1} mm";

        return result;
    }

    public async Task<string> GetForecastAsync(double latitude, double longitude, int hours = 24)
    {
        hours = Math.Clamp(hours, 1, 72);
        var url = $"{BaseUrl}/compact?lat={latitude.ToString("F4", CultureInfo.InvariantCulture)}&lon={longitude.ToString("F4", CultureInfo.InvariantCulture)}";
        var json = await GetAsync(url);

        using var doc = JsonDocument.Parse(json);
        var timeseries = doc.RootElement
            .GetProperty("properties")
            .GetProperty("timeseries");

        var lines = new List<string>
        {
            $"Værvarsel for koordinater ({latitude:F4}, {longitude:F4}) — neste {hours} timer:",
            ""
        };

        int count = 0;
        foreach (var entry in timeseries.EnumerateArray())
        {
            if (count >= hours) break;

            var time = entry.GetProperty("time").GetString();
            var details = entry
                .GetProperty("data")
                .GetProperty("instant")
                .GetProperty("details");

            var temp = details.GetProperty("air_temperature").GetDouble();
            var wind = details.GetProperty("wind_speed").GetDouble();

            string? symbol = null;
            if (entry.GetProperty("data").TryGetProperty("next_1_hours", out var n1h))
            {
                if (n1h.GetProperty("summary").TryGetProperty("symbol_code", out var sym))
                    symbol = sym.GetString();
            }

            var weatherStr = symbol != null ? SymbolToNorwegian(symbol) : "-";
            lines.Add($"{time}: {temp:F1}°C, vind {wind:F1} m/s — {weatherStr}");
            count++;
        }

        return string.Join("\n", lines);
    }

    private static string WindDirectionToNorwegian(double degrees) => degrees switch
    {
        >= 337.5 or < 22.5 => "nord",
        >= 22.5 and < 67.5 => "nordøst",
        >= 67.5 and < 112.5 => "øst",
        >= 112.5 and < 157.5 => "sørøst",
        >= 157.5 and < 202.5 => "sør",
        >= 202.5 and < 247.5 => "sørvest",
        >= 247.5 and < 292.5 => "vest",
        _ => "nordvest"
    };

    private static string SymbolToNorwegian(string code)
    {
        if (code.Contains("clearsky")) return "Klart";
        if (code.Contains("fair")) return "Lettskyet";
        if (code.Contains("partlycloudy")) return "Delvis skyet";
        if (code.Contains("cloudy")) return "Overskyet";
        if (code.Contains("fog")) return "Tåke";
        if (code.Contains("heavyrain") && code.Contains("thunder")) return "Kraftig regn med torden";
        if (code.Contains("rain") && code.Contains("thunder")) return "Regn med torden";
        if (code.Contains("thunder")) return "Torden";
        if (code.Contains("heavyrain")) return "Kraftig regn";
        if (code.Contains("lightrain")) return "Lett regn";
        if (code.Contains("rain")) return "Regn";
        if (code.Contains("heavysleet")) return "Kraftig sludd";
        if (code.Contains("lightsleet")) return "Lett sludd";
        if (code.Contains("sleet")) return "Sludd";
        if (code.Contains("heavysnow")) return "Kraftig snø";
        if (code.Contains("lightsnow")) return "Lett snø";
        if (code.Contains("snow")) return "Snø";
        if (code.Contains("drizzle")) return "Yr";
        return code;
    }
}
