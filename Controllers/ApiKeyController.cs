using Microsoft.AspNetCore.Mvc;
using WeatherIntelligencePlatform.Services;

namespace WeatherIntelligencePlatform.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApiKeyController : ControllerBase
{
    private readonly ApiKeyService _apiKeyService;

    public ApiKeyController(ApiKeyService apiKeyService)
    {
        _apiKeyService = apiKeyService;
    }

    [HttpPost("generate")]
    public IActionResult GenerateApiKey([FromQuery] string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest(new { error = "Kullanıcı adı gerekli." });

        var key = _apiKeyService.GenerateApiKey(userId);
        return Ok(new { apiKey = key, userId });
    }

    [HttpGet("list")]
    public IActionResult ListKeys()
    {
        return Ok(new { keys = _apiKeyService.GetAllKeys() });
    }

    [HttpDelete("revoke")]
    public IActionResult RevokeApiKey([FromQuery] string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return BadRequest(new { error = "API Key gerekli." });

        _apiKeyService.RevokeApiKey(apiKey);
        return Ok(new { message = "API Key iptal edildi." });
    }
}