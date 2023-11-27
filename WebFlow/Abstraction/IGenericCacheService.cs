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
    /// Stores an object into a Redis database cache with a designated key construction assuming [CacheKey]:ClassName format.
    /// </summary>
    /// <param name="genericObject">Object instance to be stored into cache.</param>
    /// <param name="expiry">Optional. Specifies the duration for which the object should remain in the cache. Default value is null indicating no expiry time.</param>
    /// <param name="when">Optional. Indicates the scenarios where this operation should be performed. Can be configured to accommodate different contexts. Default is set to always.</param>
    /// <typeparam name="T">Type of the object to be cached. It should be serializable.</typeparam>
    void CacheObject<T>(T genericObject, TimeSpan? expiry = null, When when = When.Always);

    /// <summary>
    /// Retrieves all cached instances of the specified type from the cache.
    /// </summary>
    /// <param name="genericObject">Type of the objects to be fetched from the cache.</param>
    /// <returns>A list of string representations of all cached instances of the specified type.</returns>
    List<string> FetchAll(Type genericObject);
    
    /// <summary>
    /// Retrieves a specific object from the cache using its associated key.
    /// </summary>
    /// <param name="genericObject">Type of the object to be fetched.</param>
    /// <param name="key">Unique key associated with the object in the cache.</param>
    /// <returns>The cached object serialized as string if found, otherwise null.</returns>
    string? FetchObject(Type genericObject, string key);
    
    /// <summary>
    /// Fetches objects from the cache that have key values closely resembling the provided guess.
    /// </summary>
    /// <param name="genericObject">Type of the objects to be fetched.</param>
    /// <param name="guess">Estimate of the cacheKey associated with the desired objects.</param>
    /// <returns>List of objects serialized as strings that have keys resembling the provided guess.</returns>
    List<string> FetchNearest(Type genericObject, string guess);

    /// <summary>
    /// Updates a specific object within the cache.
    /// </summary>
    /// <param name="genericObject">Updated version of the object to be stored into cache.</param>
    /// <typeparam name="T">Type of the object to be updated in the cache. It should be serializable.</typeparam>
    void UpdateObject<T>(T genericObject);
    
    /// <summary>
    /// Deletes a specific object from the cache using its associated key.
    /// </summary>
    /// <param name="key">Unique key associated with the object in the cache.</param>
    /// <returns>True if deletion was successful, false otherwise.</returns>
    bool DeleteObject(string key);
    
    /// <summary>
    /// Clears and repopulates the cache with a new set of objects.
    /// </summary>
    /// <param name="genericObjects">List of new objects to be stored into cache after clearance.</param>
    /// <returns>Task representing the asynchronous operation of cache refreshing.</returns>
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