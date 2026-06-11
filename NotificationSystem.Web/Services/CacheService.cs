using Microsoft.Extensions.Caching.Distributed;
using NotificationSystem.Web.Interfaces;
using System.Text.Json;

namespace NotificationSystem.Web.Services;

public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly TimeSpan _defaultExpiration;

    public CacheService(IDistributedCache cache, IConfiguration configuration)
    {
        _cache = cache;
        var minutes = int.Parse(configuration["Redis:DefaultExpirationMinutes"] ?? "5");
        _defaultExpiration = TimeSpan.FromMinutes(minutes);
        Console.WriteLine($"[Cache] CacheService создан. Время кеша по умолчанию: {_defaultExpiration.TotalMinutes} мин.");
    }

    public async Task<T> GetAsync<T>(string key) where T : class
{
    try
    {
        var cachedValue = await _cache.GetStringAsync(key);
        if (cachedValue == null)
        {
            Console.WriteLine($"[Cache] Промах — ключ '{key}' не найден");
            return null;
        }

        Console.WriteLine($"[Cache] Попадание — ключ '{key}'");
        return JsonSerializer.Deserialize<T>(cachedValue);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Cache] Ошибка при чтении: {ex.Message}");
        return null;
    }
}

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration
            };

            var serialized = JsonSerializer.Serialize(value);
            await _cache.SetStringAsync(key, serialized, options);
            Console.WriteLine($"[Cache] Сохранено — ключ '{key}' ({options.AbsoluteExpirationRelativeToNow})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Cache] Ошибка при записи: {ex.Message}");
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _cache.RemoveAsync(key);
            Console.WriteLine($"[Cache] Удалён ключ '{key}'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Cache] Ошибка при удалении: {ex.Message}");
        }
    }
}