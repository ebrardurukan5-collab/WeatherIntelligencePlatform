using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;

namespace WeatherIntelligencePlatform.Services;

public class ApiKeyService
{
    private readonly IMemoryCache _cache;
    private readonly Dictionary<string, string> _apiKeys = new();

    public ApiKeyService(IMemoryCache cache)
    {
        _cache = cache;
        _apiKeys["dev-key-123"] = "developer";
    }

    public string GenerateApiKey(string userId)
    {
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("/", "_").Replace("+", "-").Substring(0, 32);
        _apiKeys[key] = userId;
        return key;
    }

    public bool ValidateApiKey(string apiKey)
    {
        return _apiKeys.ContainsKey(apiKey);
    }

    public string GetUserId(string apiKey)
    {
        return _apiKeys.GetValueOrDefault(apiKey);
    }

    public void RevokeApiKey(string apiKey)
    {
        _apiKeys.Remove(apiKey);
    }

    public List<string> GetAllKeys()
    {
        return _apiKeys.Keys.ToList();
    }
}