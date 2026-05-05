using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WeatherWeb.Services;

namespace WeatherWeb.Pages;

[Authorize]
public class IndexModel(ClaudeService claude) : PageModel
{
    public string? Question { get; private set; }
    public string? Answer { get; private set; }
    public string? Error { get; private set; }

    public async Task<IActionResult> OnPostAsync(string question)
    {
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
