using Serilog;
using Microsoft.EntityFrameworkCore;
using WeatherIntelligencePlatform.Data;
using WeatherIntelligencePlatform.Services;
using WeatherIntelligencePlatform.Services.Providers;
using WeatherIntelligencePlatform.Repositories;
using WeatherIntelligencePlatform.Middlewares;

// ===== BELLEK OPTİMİZASYONU (EXIT 139 ÇÖZÜMÜ) =====
AppContext.SetSwitch("System.GC.Server", false);
AppContext.SetSwitch("System.GC.Concurrent", true);

var builder = WebApplication.CreateBuilder(args);

// ===== Logging =====
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console()
        .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day);
});

// ===== Servisler =====
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.WriteIndented = false;
    });

builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

// ===== Veritabanı =====
builder.Services.AddDbContext<WeatherDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ===== Repository =====
builder.Services.AddScoped<IWeatherRepository, WeatherRepository>();

// ===== HTTP Client =====
builder.Services.AddHttpClient<GeocodingService>();
builder.Services.AddHttpClient<WeatherApiProvider>();

// ===== Provider =====
builder.Services.AddScoped<WeatherApiProvider>();
builder.Services.AddScoped<WeatherProviderOrchestrator>();

// ===== Business Servisler =====
builder.Services.AddScoped<WeatherBusinessService>();
builder.Services.AddScoped<RouteWeatherService>();

// ===== STATS SERVİSİ =====
builder.Services.AddSingleton<StatsService>();

// ===== NOTIFICATION SERVİSİ =====
builder.Services.AddScoped<NotificationService>();

// ===== CORS =====
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// ===== Middleware =====
app.UseMiddleware<ExceptionMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.MapHealthChecks("/health");
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// ===== VERİTABANI MIGRATION (OTOMATİK) =====
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();
    dbContext.Database.Migrate();
}

app.Run();