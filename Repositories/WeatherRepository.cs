using Microsoft.EntityFrameworkCore;
using WeatherIntelligencePlatform.Data;
using WeatherIntelligencePlatform.Models;

namespace WeatherIntelligencePlatform.Repositories;

public class WeatherRepository : IWeatherRepository
{
    private readonly WeatherDbContext _context;

    public WeatherRepository(WeatherDbContext context)
    {
        _context = context;
    }

    public async Task AddQueryLogAsync(WeatherQueryLog log)
    {
        var logs = await _context.WeatherQueryLogs
            .OrderByDescending(l => l.QueriedAt)
            .ToListAsync();

        if (logs.Count >= 10)
        {
            var toDelete = logs.Skip(9).ToList();
            _context.WeatherQueryLogs.RemoveRange(toDelete);
        }

        _context.WeatherQueryLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<List<WeatherQueryLog>> GetAllLogsAsync()
    {
        return await _context.WeatherQueryLogs
            .OrderByDescending(l => l.QueriedAt)
            .ToListAsync();
    }

    public async Task<List<WeatherQueryLog>> GetRecentLogsAsync(int count)
    {
        return await _context.WeatherQueryLogs
            .OrderByDescending(l => l.QueriedAt)
            .Take(count)
            .ToListAsync();
    }
}