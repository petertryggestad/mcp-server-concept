using System.Text.Json;

namespace WeatherWeb.Services;

public record GeocodingResult(double Latitude, double Longitude, string DisplayName);

public class GeocodingService
{
    private readonly HttpClient _client;

    public GeocodingService(IHttpClientFactory factory)
    {
        _client = factory.CreateClient("default");
    }

    public async Task<GeocodingResult?> GeocodeAsync(string location)
    {
        var url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(location)}&format=json&limit=1";
        var json = await _client.GetStringAsync(url);
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.GetArrayLength() == 0) return null;

        var first = doc.RootElement[0];
        var lat = double.Parse(first.GetProperty("lat").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
        var lon = double.Parse(first.GetProperty("lon").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
        var displayName = first.GetProperty("display_name").GetString()!;

        return new GeocodingResult(lat, lon, displayName);
    }
}
