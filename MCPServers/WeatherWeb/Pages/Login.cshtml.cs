using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace WeatherWeb.Pages;

public class LoginModel(IConfiguration config) : PageModel
{
    public string? Error { get; private set; }

    public async Task<IActionResult> OnPostAsync(string username, string password)
    {
        var validUser = config["Auth:Username"];
        var validPass = config["Auth:Password"];

        if (username != validUser || password != validPass)
        {
            Error = "Feil brukernavn eller passord.";
            return Page();
        }

        var claims = new List<Claim> { new(ClaimTypes.Name, username) };
        var identity = new ClaimsIdentity(claims, "Cookies");
        await HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(identity));

        return RedirectToPage("/Index");
    }
}
