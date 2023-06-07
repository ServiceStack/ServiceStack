using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MyApp.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    [FromQuery]
    public string? Code { get; set; }
    [FromQuery]
    public string? Role { get; set; }
    [FromQuery]
    public string? Permission { get; set; }
    public string? RequestId { get; set; }
    public int Status { get; set; } = 500;

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    private readonly ILogger<ErrorModel> _logger;

    public ErrorModel(ILogger<ErrorModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        Status = Code != null && int.TryParse(Code, out var code) ? code : Response.StatusCode;
    }
}
