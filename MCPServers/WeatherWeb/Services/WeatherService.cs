using System.Globalization;
using System.Text.Json;

namespace WeatherWeb.Services;

public record CurrentWeather(
    double Temperature,
    double WindSpeed,
    string WindDirection,
    double Humidity,
    string WeatherType,
    double? Precipitation
);

public record ForecastEntry(string Time, double Temperature, double WindSpeed, string WeatherType);

public class WeatherService
{
    private readonly HttpClient _client;
    private const string BaseUrl = "https://api.met.no/weatherapi/locationforecast/2.0";

    public WeatherService(IHttpClientFactory factory)
    {
        _client = factory.CreateClient("default");
    }

    public async Task<CurrentWeather?> GetCurrentWeatherAsync(double lat, double lon)
    {
        var url = $"{BaseUrl}/compact?lat={lat.ToString("F4", CultureInfo.InvariantCulture)}&lon={lon.ToString("F4", CultureInfo.InvariantCulture)}";
        var json = await _client.GetStringAsync(url);
        using var doc = JsonDocument.Parse(json);

        var timeseries = doc.RootElement.GetProperty("properties").GetProperty("timeseries");
        if (timeseries.GetArrayLength() == 0) return null;

        var current = timeseries[0];
        var details = current.GetProperty("data").GetProperty("instant").GetProperty("details");

        var temp = details.GetProperty("air_temperature").GetDouble();
        var wind = details.GetProperty("wind_speed").GetDouble();
        var windDir = details.GetProperty("wind_from_direction").GetDouble();
        var humidity = details.GetProperty("relative_humidity").GetDouble();

        string? symbolCode = null;
        double? precip = null;
        if (current.GetProperty("data").TryGetProperty("next_1_hours", out var n1h))
        {
            if (n1h.GetProperty("summary").TryGetProperty("symbol_code", out var sym))
                symbolCode = sym.GetString();
            if (n1h.GetProperty("details").TryGetProperty("precipitation_amount", out var pa))
                precip = pa.GetDouble();
        }

        return new CurrentWeather(temp, wind, WindDirection(windDir), humidity,
            symbolCode != null ? SymbolToNorwegian(symbolCode) : "Ukjent", precip);
    }

    public async Task<List<ForecastEntry>> GetForecastAsync(double lat, double lon, int hours = 12)
    {
        var url = $"{BaseUrl}/compact?lat={lat.ToString("F4", CultureInfo.InvariantCulture)}&lon={lon.ToString("F4", CultureInfo.InvariantCulture)}";
        var json = await _client.GetStringAsync(url);
        using var doc = JsonDocument.Parse(json);

        var timeseries = doc.RootElement.GetProperty("properties").GetProperty("timeseries");
        var result = new List<ForecastEntry>();

        foreach (var entry in timeseries.EnumerateArray().Skip(1).Take(hours))
        {
            var time = entry.GetProperty("time").GetString()!;
            var details = entry.GetProperty("data").GetProperty("instant").GetProperty("details");
            var temp = details.GetProperty("air_temperature").GetDouble();
            var wind = details.GetProperty("wind_speed").GetDouble();

            string symbol = "-";
            if (entry.GetProperty("data").TryGetProperty("next_1_hours", out var n1h))
                if (n1h.GetProperty("summary").TryGetProperty("symbol_code", out var sym))
                    symbol = SymbolToNorwegian(sym.GetString()!);

            var localTime = DateTime.Parse(time).ToString("HH:mm");
            result.Add(new ForecastEntry(localTime, temp, wind, symbol));
        }

        return result;
    }

    private static string WindDirection(double deg) => deg switch
    {
        >= 337.5 or < 22.5 => "N",
        >= 22.5 and < 67.5 => "NØ",
        >= 67.5 and < 112.5 => "Ø",
        >= 112.5 and < 157.5 => "SØ",
        >= 157.5 and < 202.5 => "S",
        >= 202.5 and < 247.5 => "SV",
        >= 247.5 and < 292.5 => "V",
        _ => "NV"
    };

    private static string SymbolToNorwegian(string code)
    {
        if (code.Contains("clearsky")) return "Klart";
        if (code.Contains("fair")) return "Lettskyet";
        if (code.Contains("partlycloudy")) return "Delvis skyet";
        if (code.Contains("cloudy")) return "Overskyet";
        if (code.Contains("fog")) return "Tåke";
        if (code.Contains("thunder")) return "Torden";
        if (code.Contains("heavyrain")) return "Kraftig regn";
        if (code.Contains("lightrain")) return "Lett regn";
        if (code.Contains("rain")) return "Regn";
        if (code.Contains("snow")) return "Snø";
        if (code.Contains("sleet")) return "Sludd";
        if (code.Contains("drizzle")) return "Yr";
        return code;
    }
}
