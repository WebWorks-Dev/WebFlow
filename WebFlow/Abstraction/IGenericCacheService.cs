using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using WebFlow.Attributes.Cache;
using WebFlow.Logic;
using WebFlow.Models;

namespace WebFlow.Caching;

public interface IGenericCacheService
{
    /// <summary>
    /// Caches an object into a redis database, the key being [CacheKey]:ClassName
    /// </summary>
    /// <param name="genericObject">The object you're trying to cache</param>
    /// <param name="expiry">How long the object should be stored in the cache for</param>
    /// <param name="when">Indicates when this operation should be performed (only some variations are legal in a given context).</param>
    /// <typeparam name="T">The generic object</typeparam>
    void CacheObject<T>(T genericObject, TimeSpan? expiry = null, When when = When.Always);

    /// <summary>
    ///  Fetches all cached variations of this type, i.e all cached item listings
    /// </summary>
    /// <param name="genericObject">The cache object</param>
    /// <returns></returns>
    List<string> FetchAll(Type genericObject);
    
    /// <summary>
    /// Fetches the cached object
    /// </summary>
    /// <param name="genericObject">The cached object</param>
    /// <param name="key">The cache-key of the object, i.e a user Id</param>
    /// <returns></returns>
    string? FetchObject(Type genericObject, string key);
    
    /// <summary>
    /// Fetches the item based on the nearest guess to the cacheKey, i.e can be used in item searches when the key is set to the item Name
    /// </summary>
    /// <param name="genericObject">The cached object</param>
    /// <param name="guess">The guess is based on the cacheKey of the cacheObject</param>
    /// <returns></returns>
    List<string> FetchNearest(Type genericObject, string guess);

    /// <summary>
    /// Updates the item within the cache
    /// </summary>
    /// <param name="genericObject">The cache object you want to update</param>
    /// <typeparam name="T"></typeparam>
    void UpdateObject<T>(T genericObject);
    
    /// <summary>
    /// Deletes the item from the cache
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    bool DeleteObject(string key);
    
    /// <summary>
    /// Refreshes the cache with the set of objects provided, i.e refreshes the cache of item listings
    /// </summary>
    /// <param name="genericObjects"></param>
    /// <returns></returns>
    Task RefreshCacheAsync(List<Type> genericObjects);
}

public static class RedisConnectionManager
{
    private static Lazy<ConnectionMultiplexer> _lazyConnection = null!;

    internal static ConnectionMultiplexer Connection => _lazyConnection.Value;

    public static void RegisterCachingService(this IServiceCollection serviceCollection, Assembly assembly, string connectionString)
    {
        _lazyConnection = new(() => ConnectionMultiplexer.Connect(connectionString));
        
        foreach (var type in assembly.GetTypes())
        {
            PropertyInfo[] classProperties = type.GetProperties();
            if (classProperties.Length is 0)
                continue;
            
            PropertyInfo? cacheKey = classProperties.FirstOrDefault(x => x.GetCustomAttribute<CacheKeyAttribute>() is not null);
            if(cacheKey is null)
                continue;

            ServicesConfiguration.CachePropertiesMap.Add(type, new CacheObject(type.Name, cacheKey));
        }
        
        serviceCollection.AddSingleton(typeof(IGenericCacheService), typeof(GenericCacheImplementation));
    }
}