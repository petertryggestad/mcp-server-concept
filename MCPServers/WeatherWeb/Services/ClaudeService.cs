using System.Text;
using System.Text.Json;

namespace WeatherWeb.Services;

public class ClaudeService
{
    private readonly HttpClient _client;
    private readonly string _apiKey;
    private readonly GeocodingService _geocoding;
    private readonly McpWeatherClient _mcp;

    public ClaudeService(IHttpClientFactory factory, IConfiguration config,
        GeocodingService geocoding, McpWeatherClient mcp)
    {
        _client    = factory.CreateClient();
        _apiKey    = config["Anthropic:ApiKey"] ?? throw new InvalidOperationException("Anthropic:ApiKey ikke konfigurert");
        _geocoding = geocoding;
        _mcp       = mcp;
    }

    public async Task<string> AskAsync(string question)
    {
        // Geocode and fetch weather before calling Claude — avoids multiple Claude round-trips
        var geo = await _geocoding.GeocodeAsync(question);

        string? weatherContext = null;
        if (geo != null)
        {
            // Fetch current weather and forecast in parallel from WeatherForecast MCP server
            var currentTask  = _mcp.GetCurrentWeatherAsync(geo.Latitude, geo.Longitude);
            var forecastTask = _mcp.GetWeatherForecastAsync(geo.Latitude, geo.Longitude, 12);
            await Task.WhenAll(currentTask, forecastTask);
            var current  = currentTask.Result;
            var forecast = forecastTask.Result;
            weatherContext = $"Sted: {geo.DisplayName}\n\nNåværende vær:\n{current}\n\nVarsel neste 12 timer:\n{forecast}";
        }

        var userContent = weatherContext != null
            ? $"Værinformasjon fra met.no/Yr (hentet via WeatherForecast MCP-server):\n{weatherContext}\n\nSpørsmål: {question}"
            : question;

        var body = new
        {
            model = "claude-sonnet-4-6",
            max_tokens = 1024,
            system = "Du er en hjelpsom værmelding-assistent. Svar alltid på norsk. Gi klare og vennlige værbeskrivelser basert på dataene du får.",
            messages = new[] { new { role = "user", content = userContent } }
        };

        var (_, content) = await SendRequestAsync(body);
        return ExtractText(content);
    }

    private async Task<(string StopReason, JsonElement Content)> SendRequestAsync(object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        request.Headers.Add("x-api-key", _apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await _client.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Claude API feil ({response.StatusCode}): {json}");

        using var doc = JsonDocument.Parse(json);
        var stopReason = doc.RootElement.GetProperty("stop_reason").GetString()!;
        var content    = doc.RootElement.GetProperty("content").Clone();
        return (stopReason, content);
    }

    private static string ExtractText(JsonElement content)
    {
        var sb = new StringBuilder();
        foreach (var block in content.EnumerateArray())
            if (block.GetProperty("type").GetString() == "text")
                sb.Append(block.GetProperty("text").GetString());
        return sb.ToString();
    }
}
