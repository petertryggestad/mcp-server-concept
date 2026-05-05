using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WeatherWeb.Services;

namespace WeatherWeb.Pages;

[Authorize]
public class IndexModel(WeatherService weather, GeocodingService geocoding) : PageModel
{
    public string? Location { get; private set; }
    public string? LocationDisplayName { get; private set; }
    public CurrentWeather? Weather { get; private set; }
    public List<ForecastEntry> Forecast { get; private set; } = [];
    public string? Error { get; private set; }

    public async Task<IActionResult> OnPostAsync(string location)
    {
        Location = location;

        var geo = await geocoding.GeocodeAsync(location);
        if (geo == null)
        {
            Error = $"Fant ikke stedet «{location}». Prøv et annet navn.";
            return Page();
        }

        LocationDisplayName = geo.DisplayName.Split(',')[0].Trim();
        Weather = await weather.GetCurrentWeatherAsync(geo.Latitude, geo.Longitude);
        Forecast = await weather.GetForecastAsync(geo.Latitude, geo.Longitude);

        return Page();
    }
}
