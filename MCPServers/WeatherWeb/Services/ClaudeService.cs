using System.Text;
using System.Text.Json;

namespace WeatherWeb.Services;

public class ClaudeService
{
    private readonly HttpClient _client;
    private readonly string _apiKey;
    private readonly GeocodingService _geocoding;
    private readonly McpWeatherClient _mcp;

    private static readonly object[] Tools =
    [
        new {
            name = "geocode",
            description = "Finner geografiske koordinater (breddegrad og lengdegrad) for et stednavn.",
            input_schema = new {
                type = "object",
                properties = new {
                    location = new { type = "string", description = "Stedsnavn, f.eks. 'Oslo' eller 'Paris'" }
                },
                required = new[] { "location" }
            }
        },
        new {
            name = "get_current_weather",
            description = "Henter nåværende vær fra met.no/Yr for en geografisk posisjon.",
            input_schema = new {
                type = "object",
                properties = new {
                    latitude  = new { type = "number", description = "Breddegrad" },
                    longitude = new { type = "number", description = "Lengdegrad" }
                },
                required = new[] { "latitude", "longitude" }
            }
        },
        new {
            name = "get_weather_forecast",
            description = "Henter værvarsel for de neste timene fra met.no/Yr for en geografisk posisjon.",
            input_schema = new {
                type = "object",
                properties = new {
                    latitude  = new { type = "number", description = "Breddegrad" },
                    longitude = new { type = "number", description = "Lengdegrad" },
                    hours     = new { type = "integer", description = "Antall timer fremover (standard: 24, maks: 72)" }
                },
                required = new[] { "latitude", "longitude" }
            }
        }
    ];

    public ClaudeService(IHttpClientFactory factory, IConfiguration config,
        GeocodingService geocoding, McpWeatherClient mcp)
    {
        _client   = factory.CreateClient();
        _apiKey   = config["Anthropic:ApiKey"] ?? throw new InvalidOperationException("Anthropic:ApiKey ikke konfigurert");
        _geocoding = geocoding;
        _mcp      = mcp;
    }

    public async Task<string> AskAsync(string question)
    {
        var messages = new List<object> { new { role = "user", content = question } };

        for (var i = 0; i < 6; i++)
        {
            var body = new
            {
                model = "claude-sonnet-4-6",
                max_tokens = 1024,
                system = "Du er en hjelpsom værmelding-assistent. Svar alltid på norsk. Bruk verktøyene til å hente værinformasjon fra met.no/Yr via MCP-serveren, og gi klare og vennlige værbeskrivelser.",
                tools = Tools,
                messages
            };

            var (stopReason, content) = await SendRequestAsync(body);

            if (stopReason != "tool_use")
                return ExtractText(content);

            messages.Add(new { role = "assistant", content });

            var toolResults = new List<object>();
            foreach (var block in content.EnumerateArray())
            {
                if (block.GetProperty("type").GetString() != "tool_use") continue;

                var toolId   = block.GetProperty("id").GetString()!;
                var toolName = block.GetProperty("name").GetString()!;
                var input    = block.GetProperty("input");

                var result = await ExecuteToolAsync(toolName, input);
                toolResults.Add(new { type = "tool_result", tool_use_id = toolId, content = result });
            }

            messages.Add(new { role = "user", content = toolResults });
        }

        throw new Exception("Fikk ikke svar fra Claude etter for mange verktøykall.");
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

    private async Task<string> ExecuteToolAsync(string name, JsonElement input)
    {
        switch (name)
        {
            case "geocode":
            {
                var location = input.GetProperty("location").GetString()!;
                var geo = await _geocoding.GeocodeAsync(location);
                return geo is null
                    ? "Fant ikke stedet."
                    : $"Breddegrad: {geo.Latitude}, Lengdegrad: {geo.Longitude}, Stedsnavn: {geo.DisplayName}";
            }
            case "get_current_weather":
            {
                var lat = input.GetProperty("latitude").GetDouble();
                var lon = input.GetProperty("longitude").GetDouble();
                return await _mcp.GetCurrentWeatherAsync(lat, lon);
            }
            case "get_weather_forecast":
            {
                var lat   = input.GetProperty("latitude").GetDouble();
                var lon   = input.GetProperty("longitude").GetDouble();
                var hours = input.TryGetProperty("hours", out var h) ? h.GetInt32() : 24;
                return await _mcp.GetWeatherForecastAsync(lat, lon, hours);
            }
            default:
                return $"Ukjent verktøy: {name}";
        }
    }
}
