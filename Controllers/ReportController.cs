using Microsoft.AspNetCore.Mvc;
using WeatherIntelligencePlatform.Services;
using System.Text; // ← EKLENDİ

namespace WeatherIntelligencePlatform.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly ReportService _reportService;

    public ReportsController(ReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("download")]
    public async Task<IActionResult> DownloadReport([FromQuery] string type, [FromQuery] string? city = null)
    {
        if (type == "csv")
        {
            var csv = await _reportService.GenerateCsvReport(city);
            return Content(csv, "text/csv", Encoding.UTF8);
        }
        else if (type == "html")
        {
            var html = await _reportService.GenerateHtmlReport(city);
            return Content(html, "text/html", Encoding.UTF8);
        }

        return BadRequest(new { error = "Geçersiz rapor türü. csv veya html kullanın." });
    }
}