using System.Text;
using System.Text.Json;

namespace WeatherWeb.Services;

public class ClaudeService
{
    private readonly HttpClient _client;
    private readonly string _apiKey;
    private const string McpServerUrl = "https://weatherforecast.salmonforest-88c883e8.norwayeast.azurecontainerapps.io/mcp";

    public ClaudeService(IHttpClientFactory factory, IConfiguration config)
    {
        _client = factory.CreateClient();
        _apiKey = config["Anthropic:ApiKey"] ?? throw new InvalidOperationException("Anthropic:ApiKey ikke konfigurert");
    }

    public async Task<string> AskAsync(string question)
    {
        var body = new
        {
            model = "claude-sonnet-4-6",
            max_tokens = 1024,
            mcp_servers = new[]
            {
                new { type = "url", url = McpServerUrl, name = "weatherforecast" }
            },
            messages = new[]
            {
                new { role = "user", content = question }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        request.Headers.Add("x-api-key", _apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await _client.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Claude API feil ({response.StatusCode}): {json}");

        using var doc = JsonDocument.Parse(json);
        var sb = new StringBuilder();
        foreach (var block in doc.RootElement.GetProperty("content").EnumerateArray())
        {
            if (block.GetProperty("type").GetString() == "text")
                sb.Append(block.GetProperty("text").GetString());
        }

        return sb.ToString();
    }
}
