using System.Globalization;

namespace WeatherWeb.Services;

public class McpWeatherClient
{
    private readonly HttpClient _client;
    private readonly string _baseUrl;

    public McpWeatherClient(IHttpClientFactory factory, string baseUrl)
    {
        _client = factory.CreateClient("default");
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public async Task<string> GetCurrentWeatherAsync(double lat, double lon)
    {
        var url = $"{_baseUrl}/weather/current?lat={lat.ToString(CultureInfo.InvariantCulture)}&lon={lon.ToString(CultureInfo.InvariantCulture)}";
        return await _client.GetStringAsync(url);
    }

    public async Task<string> GetWeatherForecastAsync(double lat, double lon, int hours = 24)
    {
        var url = $"{_baseUrl}/weather/forecast?lat={lat.ToString(CultureInfo.InvariantCulture)}&lon={lon.ToString(CultureInfo.InvariantCulture)}&hours={hours}";
        return await _client.GetStringAsync(url);
    }
}
