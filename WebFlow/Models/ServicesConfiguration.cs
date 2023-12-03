using System.Reflection;
using WebFlow.Attributes;
using WebFlow.Authorization;

namespace WebFlow.Models;

internal static class ServicesConfiguration
{
    /*internal static readonly List<Type> AssemblyTypes = Assembly.GetExecutingAssembly()
        .GetTypes()
        .ToList();*/
    
    /*
    internal static JwtConfig JwtConfig { get; set; }
    */

    internal static string? ReCaptchaKey;
    
    internal static AuthorizationType AuthorizationType;
    internal static readonly Dictionary<Type, Dictionary<string, List<PropertyInfo>>> AuthenticationPropertiesMap = new();

    internal static bool IsEmailAuthEnabled = false;
    internal static readonly Dictionary<Type, Dictionary<string, List<PropertyInfo>>> EmailFieldsMap = new();
    
    internal static readonly Dictionary<Type, CacheObject> CachePropertiesMap = new();
}

internal record CacheObject(string ClassName, PropertyInfo CacheKeyProperty);