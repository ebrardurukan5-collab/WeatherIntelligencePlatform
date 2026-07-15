using System.Collections.Concurrent;

namespace WeatherIntelligencePlatform.Services;

public class RateLimitingService
{
    private readonly ConcurrentDictionary<string, (int Count, DateTime ResetTime)> _requests = new();
    private readonly int _maxRequests = 100;
    private readonly TimeSpan _window = TimeSpan.FromMinutes(1);

    public bool IsAllowed(string clientId)
    {
        var now = DateTime.UtcNow;
        var key = clientId ?? "anonymous";

        _requests.AddOrUpdate(key,
            (1, now.Add(_window)),
            (_, existing) =>
            {
                if (now > existing.ResetTime)
                    return (1, now.Add(_window));
                return (existing.Count + 1, existing.ResetTime);
            });

        var current = _requests[key];
        return current.Count <= _maxRequests;
    }

    public int GetRemaining(string clientId)
    {
        var key = clientId ?? "anonymous";
        if (!_requests.TryGetValue(key, out var current))
            return _maxRequests;
        return Math.Max(0, _maxRequests - current.Count);
    }
}