using System.Text;
using System.Text.Json;

namespace WeatherWeb.Services;

public class ClaudeService
{
    private readonly HttpClient _client;
    private readonly string _apiKey;

    public ClaudeService(IHttpClientFactory factory, IConfiguration config)
    {
        _client = factory.CreateClient();
        _apiKey = config["Anthropic:ApiKey"] ?? throw new InvalidOperationException("Anthropic:ApiKey ikke konfigurert");
    }

    public async Task<string> AskAsync(string question, string? weatherContext = null)
    {
        var content = weatherContext != null
            ? $"Værinformasjon fra met.no:\n{weatherContext}\n\nSpørsmål: {question}"
            : question;

        var body = new
        {
            model = "claude-sonnet-4-6",
            max_tokens = 1024,
            system = "Du er en hjelpsom værmelding-assistent. Svar alltid på norsk. Gi klare og vennlige værbeskrivelser basert på dataene du får.",
            messages = new[] { new { role = "user", content } }
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
