using System.Collections.Concurrent;

namespace WeatherIntelligencePlatform.Services;

public class StatsService
{
    private int _totalRequests = 0;
    private int _successRequests = 0;
    private int _errorRequests = 0;
    private readonly ConcurrentDictionary<string, int> _cityRequests = new();
    private readonly List<double> _responseTimes = new();
    private DateTime _startTime = DateTime.Now;

    public void RecordRequest(string city, bool isSuccess, double responseTime)
    {
        Interlocked.Increment(ref _totalRequests);
        if (isSuccess) Interlocked.Increment(ref _successRequests);
        else Interlocked.Increment(ref _errorRequests);

        _cityRequests.AddOrUpdate(city, 1, (_, count) => count + 1);
        lock (_responseTimes) { _responseTimes.Add(responseTime); }
    }

    public object GetStats()
    {
        lock (_responseTimes)
        {
            var avgTime = _responseTimes.Count > 0 ? Math.Round(_responseTimes.Average(), 2) : 0;
            var successRate = _totalRequests > 0
                ? Math.Round((double)_successRequests / _totalRequests * 100, 2)
                : 100;

            return new
            {
                uptime = (DateTime.Now - _startTime).ToString(@"dd\.hh\:mm\:ss"),
                totalRequests = _totalRequests,
                successRate,
                averageResponseTime = avgTime,
                topCities = _cityRequests.OrderByDescending(x => x.Value).Take(5)
                    .Select(x => new { city = x.Key, count = x.Value })
            };
        }
    }
}