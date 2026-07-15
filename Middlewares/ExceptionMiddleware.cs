using System.Text.Json;

namespace WeatherIntelligencePlatform.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Global hata yakalandı: {Message}", ex.Message);
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            var response = new { error = "Bir hata oluştu. Lütfen daha sonra tekrar deneyin." };
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}