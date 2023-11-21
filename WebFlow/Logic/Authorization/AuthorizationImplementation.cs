using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Caching.Memory;
using WebFlow.Attributes;
using WebFlow.Extensions;
using WebFlow.Passwords;
using WebFlow.Models;

namespace WebFlow.Authorization;

internal partial class WebFlowAuthorizationImplementation : IWebFlowAuthorizationService
{
    private readonly IPasswordHashService _passwordHashService;
    private readonly JwtConfig _jwtConfig;
    private readonly IMemoryCache _memoryCache;

    public WebFlowAuthorizationImplementation(IPasswordHashService passwordHashService, JwtConfig jwtConfig, IMemoryCache memoryCache)
    {
        _passwordHashService = passwordHashService;
        _jwtConfig = jwtConfig;
        _memoryCache = memoryCache;
    }

    private static List<PropertyInfo>? FetchMappedProperty<T>(Dictionary<Type, Dictionary<string, List<PropertyInfo>>> map, T objectValue, string requestedObject)
    {
        if (!map.TryGetValue(typeof(T), out var mapProperties))
            return null;

        return mapProperties.TryGetValue(requestedObject, out var propertyInfo)
            ? propertyInfo 
            : null;
    }

    private static T? FetchDatabaseObject<T>(List<PropertyInfo> propertyInfos, T authenticationObject, DbContext dbContext) where T : class
    {
        IQueryable<T> query = dbContext.Set<T>().AsQueryable();

        foreach (var propertyInfo in propertyInfos)
        {
            object? value = propertyInfo.GetValue(authenticationObject);
            if(value is null)
                continue;
                
            query = query.Where(e => EF.Property<object>(e, propertyInfo.Name) == propertyInfo.GetValue(authenticationObject));
        }
        
        return query.FirstOrDefault();
    }

    private void SetPassword<T>(T authenticationObject)
    {
        PropertyInfo? passwordProperty = FetchMappedProperty(ServicesConfiguration.AuthenticationPropertiesMap, authenticationObject, "password")?[0];
        if (passwordProperty is null) 
            return;
        
        var passwordAttribute = (PasswordAttribute)Attribute.GetCustomAttribute(passwordProperty, typeof(PasswordAttribute))!;
        var passwordValue = (string?)passwordProperty.GetValue(authenticationObject);

        if (passwordValue is null) 
            return;
        
        string hashedValue = _passwordHashService.CreateHash(passwordValue, passwordAttribute.HashType);
        passwordProperty.SetValue(authenticationObject, hashedValue);
    }

    public Result<T?> RegisterUser<T>(DbContext dbContext, T authenticationObject) where T : class
    {
        Type objectType = typeof(T);
        
        IEntityType? entityType = dbContext.Model.FindEntityType(objectType);
        if (entityType is null)
        {
            return Result<T>.Fail(new WebFlowException(AuthorizationConstants.CantFindType(objectType)));
        }

        SetPassword(authenticationObject);

        List<PropertyInfo>? uniqueProperties = FetchMappedProperty(ServicesConfiguration.AuthenticationPropertiesMap, authenticationObject, "unique_properties");
        if (uniqueProperties is not null)
        {
            T? user = FetchDatabaseObject(uniqueProperties, authenticationObject, dbContext);
            if(user is not null)
                return Result<T>.Fail(AuthorizationConstants.AccountAlreadyExists);
        }

        return ExceptionExtensions.Try<T?>(() => dbContext.Set<T>().Add(authenticationObject).Entity);
    }

    public async Task<Result<T?>> AuthenticateUserAsync<T>(DbContext dbContext, HttpContext httpContext, T authenticationObject) where T : class
    {
        Type objectType = typeof(T);

        IEntityType? entityType = dbContext.Model.FindEntityType(objectType);
        if (entityType is null)
        {
            return Result<T>.Fail(new WebFlowException(AuthorizationConstants.CantFindType(objectType)));
        }

        List<PropertyInfo>? authenticationFields = FetchMappedProperty(ServicesConfiguration.AuthenticationPropertiesMap, authenticationObject, "authentication_fields");
        if(authenticationFields is null || FetchDatabaseObject(authenticationFields, authenticationObject, dbContext) is not { } user)
        {
            return Result<T>.Fail(AuthorizationConstants.InvalidParameters);
        }
        
        PropertyInfo? userPassword = FetchMappedProperty(ServicesConfiguration.AuthenticationPropertiesMap, user, "password")?[0];
        PropertyInfo? passwordPropertyRequest = FetchMappedProperty(ServicesConfiguration.AuthenticationPropertiesMap, authenticationObject, "password")?[0];
        if(passwordPropertyRequest is not null && userPassword is not null)
        {
            var passwordAttribute = (PasswordAttribute)Attribute.GetCustomAttribute(passwordPropertyRequest, typeof(PasswordAttribute))!;
        
            var hashedPasswordValue = (string?)userPassword.GetValue(user);
            var passwordValue = (string?)passwordPropertyRequest.GetValue(authenticationObject);
            if (passwordValue is null || hashedPasswordValue is null)
            {
                return Result<T>.Fail(new WebFlowException(AuthorizationConstants.ValueCantBeNull));
            }
        
            bool isPasswordValid = _passwordHashService.ValidatePassword(passwordAttribute.HashType, passwordValue, hashedPasswordValue);
            if (!isPasswordValid)
            {
                return Result<T>.Fail(AuthorizationConstants.InvalidParameters);
            }
        }

        if (!ServicesConfiguration.IsEmailAuthEnabled)
        {
            IssueAuthorizationClaims(httpContext, authenticationObject);
            
            return Result<T?>.Ok(user);
        }

        PropertyInfo? registrationTokenProperty = FetchMappedProperty(ServicesConfiguration.EmailFieldsMap, authenticationObject, "registration_token")?[0];
        if(registrationTokenProperty is null)
        {
            return Result<T>.Fail(AuthorizationConstants.AccountNotVerified);
        }

        var registrationTokenValue = (Guid?)registrationTokenProperty.GetValue(user);
        if (registrationTokenValue is null || registrationTokenValue != Guid.Empty)
        {
            return Result<T>.Fail(AuthorizationConstants.AccountNotVerified);
        }

        IssueAuthorizationClaims(httpContext, authenticationObject);
        return Result<T?>.Ok(user);
    }

    public Result LogoutUser(HttpContext httpContext)
    {
        string? sessionId = httpContext.Request.Cookies["WebFlowSessionId"];
        if (sessionId is null)
        {
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Result.Fail(AuthorizationConstants.MissingWebFlowSessionId);
        }
        
        ClearCookies(httpContext);
        _memoryCache.Set(sessionId, true, TimeSpan.FromMinutes(15));

        return Result.Ok();
    }

    public Result RequestPasswordUpdate<T>(DbContext context, T accountObject)
    {
        return Result.Ok();
    }
    
    public Result UpdatePassword<T>(DbContext context, T accountObject)
    {
        return Result.Ok();
    }
}