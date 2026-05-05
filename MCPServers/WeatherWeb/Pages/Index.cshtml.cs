using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WeatherWeb.Services;

namespace WeatherWeb.Pages;

public record CityWeather(string City, string Country, string Flag, CurrentWeather? Weather);

[Authorize]
public class IndexModel(WeatherService weather, ClaudeService claude) : PageModel
{
    public List<CityWeather> Cities { get; private set; } = [];
    public string? Question { get; private set; }
    public string? Answer { get; private set; }
    public string? Error { get; private set; }

    private static readonly (string City, string Country, string Flag, double Lat, double Lon)[] MajorCities =
    [
        ("Tokyo",     "Japan",    "🇯🇵",  35.6762,  139.6503),
        ("Delhi",     "India",    "🇮🇳",  28.7041,   77.1025),
        ("Shanghai",  "Kina",     "🇨🇳",  31.2304,  121.4737),
        ("São Paulo", "Brasil",   "🇧🇷", -23.5505,  -46.6333),
        ("Mumbai",    "India",    "🇮🇳",  19.0760,   72.8777)
    ];

    public async Task OnGetAsync()
    {
        var tasks = MajorCities.Select(async c =>
        {
            var w = await weather.GetCurrentWeatherAsync(c.Lat, c.Lon);
            return new CityWeather(c.City, c.Country, c.Flag, w);
        });
        Cities = [.. await Task.WhenAll(tasks)];
    }

    public async Task<IActionResult> OnPostAsync(string question)
    {
        await OnGetAsync();
        Question = question;
        try
        {
            Answer = await claude.AskAsync(question);
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        return Page();
    }
}
