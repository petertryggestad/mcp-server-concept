using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace WeatherWeb.Services;

public class McpWeatherClient : IAsyncDisposable
{
    private readonly string _endpoint;
    private McpClient? _client;
    private readonly SemaphoreSlim _sem = new(1, 1);

    public McpWeatherClient(string endpoint) => _endpoint = endpoint;

    private async Task<McpClient> GetClientAsync(CancellationToken ct = default)
    {
        if (_client is not null) return _client;
        await _sem.WaitAsync(ct);
        try
        {
            if (_client is null)
            {
                var transport = new HttpClientTransport(new HttpClientTransportOptions
                {
                    Endpoint = new Uri(_endpoint),
                    TransportMode = HttpTransportMode.StreamableHttp
                });
                _client = await McpClient.CreateAsync(transport, cancellationToken: ct);
            }
            return _client;
        }
        finally { _sem.Release(); }
    }

    public async Task<string> GetCurrentWeatherAsync(double lat, double lon, CancellationToken ct = default)
    {
        var client = await GetClientAsync(ct);
        var result = await client.CallToolAsync("GetCurrentWeather",
            new Dictionary<string, object?> { ["latitude"] = lat, ["longitude"] = lon },
            cancellationToken: ct);
        return string.Concat(result.Content.OfType<TextContentBlock>().Select(c => c.Text));
    }

    public async Task<string> GetWeatherForecastAsync(double lat, double lon, int hours = 24, CancellationToken ct = default)
    {
        var client = await GetClientAsync(ct);
        var result = await client.CallToolAsync("GetWeatherForecast",
            new Dictionary<string, object?> { ["latitude"] = lat, ["longitude"] = lon, ["hours"] = hours },
            cancellationToken: ct);
        return string.Concat(result.Content.OfType<TextContentBlock>().Select(c => c.Text));
    }

    public async ValueTask DisposeAsync()
    {
        if (_client is not null)
            await _client.DisposeAsync();
    }
}
