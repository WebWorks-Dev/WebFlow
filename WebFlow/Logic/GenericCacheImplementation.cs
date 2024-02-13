using System.Text.Json;
using StackExchange.Redis;
using WebFlow.Caching;
using WebFlow.Models;

namespace WebFlow.Logic;

internal class GenericCacheImplementation : IGenericCacheService
{
    private static readonly ConnectionMultiplexer CacheClient = RedisConnectionManager.Connection;
    private readonly IDatabase _cacheDb = CacheClient.GetDatabase();

    private CacheObject? FetchCacheObject(Type objectType)
    {
        return ServicesConfiguration.CachePropertiesMap.TryGetValue(objectType, out var mapProperties)
            ? mapProperties
            : null;
    }

    private string CreateKey<T>(T genericObject)
    {
        CacheObject? cacheObject = FetchCacheObject(typeof(T));
        if (cacheObject is null)
            throw new WebFlowException(CacheConstants.ClassMustHaveKey);

        var objectValue = cacheObject.CacheKeyProperty.GetValue(genericObject);
        if (cacheObject is null)
            throw new WebFlowException(CacheConstants.KeyCantBeNull);

        return $"{cacheObject.ClassName}:{objectValue}";
    }

    private List<RedisKey>? GetKeysWithPrefix(Type genericObject)
    {
        IServer server = CacheClient.GetServer(CacheClient.GetEndPoints()[0]);
        
        CacheObject? cacheObject = FetchCacheObject(genericObject);
        
        return cacheObject is not null 
            ? server.Keys(pattern: cacheObject.ClassName + "*").ToList()
            : default;
    }

    public void CacheObject<T>(T genericObject, TimeSpan? expiry = null, When when = When.Always)
    {
        string key = CreateKey(genericObject);
        string json = JsonSerializer.Serialize(genericObject);

        _cacheDb.StringSet(key, json, expiry, when);
    }

    public List<string> FetchAll(Type genericObject)
    {
        CacheObject? cacheObject = FetchCacheObject(genericObject);
        if (cacheObject is null)
            return new List<string>();

        List<RedisKey>? keys = GetKeysWithPrefix(genericObject);
        if (keys is null || keys.Count is 0)
            return new List<string>();

        var returnValues = keys.Select(key => _cacheDb.StringGet(key.ToString())).Select(cachedValue => (string?)cachedValue).ToList();

        return returnValues;
    }

    public string? FetchObject(Type genericObject, string key)
    {
        CacheObject? cacheObject = FetchCacheObject(genericObject);
        if (cacheObject is null)
            return null;
        
        RedisValue cachedData = _cacheDb.StringGet($"{cacheObject.ClassName}:{key}");
        
        return !cachedData.IsNull
            ? cachedData.ToString()
            : null;
    }

    public List<string> FetchNearest(Type genericObject, string guess)
    {
        List<RedisKey>? keys = GetKeysWithPrefix(genericObject);
        if (keys is null)
            return new List<string>();

        CacheObject? cacheObject = FetchCacheObject(genericObject);
        if (cacheObject is null)
            return new List<string>();
        
        var nearestKeys = new List<string>();

        foreach (var key in keys)
        {
            string existingKey = key.ToString()[$"{cacheObject.ClassName}:".Length..];
            if (existingKey.Contains(guess, StringComparison.OrdinalIgnoreCase))
                nearestKeys.Add(existingKey);
        }

        return nearestKeys; 
    }
    
    public void UpdateObject<T>(T genericObject)
    {
        string key = CreateKey(genericObject);
        string json = JsonSerializer.Serialize(genericObject);
        
        _cacheDb.StringSet(key, json);
    }
    
    public bool DeleteObject(Type genericObject, string value)
    {
        CacheObject? cacheObject = FetchCacheObject(genericObject);
        
        return cacheObject is not null 
               && _cacheDb.KeyDelete($"{cacheObject.ClassName}:{value}");
    }
    
    public bool DeleteObject(string key)
    {
        return _cacheDb.KeyDelete(key);
    }
    
    public async Task RefreshCacheAsync<T>(List<T> genericObjects)
    {
        List<RedisKey>? keys = GetKeysWithPrefix(typeof(T));
        if (keys is null || keys.Count is 0)
        {
            foreach (var genericObject in genericObjects)
                CacheObject(genericObject);
            
            return;
        }

        var tasks = keys.Select(key => _cacheDb.KeyDeleteAsync(key)).Cast<Task>().ToList();
        await Task.WhenAll(tasks);
        
        foreach (var genericObject in genericObjects)
            CacheObject(genericObject);
    }
}