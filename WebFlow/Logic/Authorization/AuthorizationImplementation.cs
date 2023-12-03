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

    public WebFlowAuthorizationImplementation(IPasswordHashService passwordHashService, JwtConfig jwtConfig,
        IMemoryCache memoryCache)
    {
        _passwordHashService = passwordHashService;
        _jwtConfig = jwtConfig;
        _memoryCache = memoryCache;
    }

    private static List<PropertyInfo>? FetchMappedProperty<T>(
        Dictionary<Type, Dictionary<string, List<PropertyInfo>>> map, T objectValue, string requestedObject)
    {
        return map.TryGetValue(typeof(T), out var mapProperties) 
            ? mapProperties.GetValueOrDefault(requestedObject) 
            : null;
    }

    private static T? FetchDatabaseObject<T>(List<PropertyInfo> propertyInfos, T authenticationObject,
        DbContext dbContext) where T : class
    {
        IQueryable<T> query = dbContext.Set<T>().AsQueryable();

        foreach (var propertyInfo in propertyInfos)
        {
            object? value = propertyInfo.GetValue(authenticationObject);
            if (value is null)
                continue;

            query = query.Where(e =>
                EF.Property<object>(e, propertyInfo.Name) == propertyInfo.GetValue(authenticationObject));
        }

        return query.FirstOrDefault();
    }

    private Result SetPassword<T>(ref T authenticationObject)
    {
        PropertyInfo? passwordProperty = FetchMappedProperty(ServicesConfiguration.AuthenticationPropertiesMap, authenticationObject, "password")?[0];
        if (passwordProperty is null)
            return Result.Fail(AuthorizationConstants.FailedToReadType(typeof(T)));

        var passwordAttribute = (PasswordAttribute)Attribute.GetCustomAttribute(passwordProperty, typeof(PasswordAttribute))!;
        var passwordValue = (string?)passwordProperty.GetValue(authenticationObject);

        if (passwordValue is null)
            return Result.Fail(AuthorizationConstants.PasswordFieldValueCantBeNull);

        string hashedValue = _passwordHashService.CreateHash(passwordValue, passwordAttribute.HashType);
        passwordProperty.SetValue(authenticationObject, hashedValue);

        return Result.Ok();
    }

    public Result<T?> RegisterUser<T>(DbContext dbContext, T authenticationObject) where T : class
    {
        Type objectType = typeof(T);

        IEntityType? entityType = dbContext.Model.FindEntityType(objectType);
        if (entityType is null)
        {
            throw new WebFlowException(AuthorizationConstants.CantFindType(objectType));
        }

        SetPassword(ref authenticationObject);

        List<PropertyInfo>? uniqueProperties = FetchMappedProperty(ServicesConfiguration.AuthenticationPropertiesMap,
            authenticationObject, "unique_properties");
        if (uniqueProperties is not null)
        {
            T? user = FetchDatabaseObject(uniqueProperties, authenticationObject, dbContext);
            if (user is not null)
            {
                return Result<T>.Fail(AuthorizationConstants.AccountAlreadyExists);
            }
        }

        if (ServicesConfiguration.IsEmailAuthEnabled)
        {
            PropertyInfo? registrationTokenProperty = FetchMappedProperty(ServicesConfiguration.EmailFieldsMap,
                authenticationObject, "registration_token")?[0];

            if (registrationTokenProperty is null)
            {
                return Result<T>.Fail(AuthorizationConstants.RegistrationTokenCantBeNull);
            }

            registrationTokenProperty.SetValue(authenticationObject, Guid.NewGuid());
        }

        return ExceptionExtensions.Try<T?>(() => dbContext.Set<T>().Add(authenticationObject).Entity);
    }

    public async Task<Result<T?>> AuthenticateUserAsync<T>(DbContext dbContext, HttpContext httpContext,
        T authenticationObject) where T : class
    {
        Type objectType = typeof(T);

        IEntityType? entityType = dbContext.Model.FindEntityType(objectType);
        if (entityType is null)
        {
            throw new WebFlowException(AuthorizationConstants.CantFindType(objectType));
        }

        List<PropertyInfo>? authenticationFields = FetchMappedProperty(ServicesConfiguration.AuthenticationPropertiesMap, authenticationObject, "authentication_fields");
        if (authenticationFields is null ||
            FetchDatabaseObject(authenticationFields, authenticationObject, dbContext) is not { } user)
        {
            return Result<T>.Fail(AuthorizationConstants.InvalidParameters);
        }

        PropertyInfo? userPassword = FetchMappedProperty(ServicesConfiguration.AuthenticationPropertiesMap, user, "password")?[0];
        PropertyInfo? passwordPropertyRequest = FetchMappedProperty(ServicesConfiguration.AuthenticationPropertiesMap, authenticationObject, "password")?[0];
        if (passwordPropertyRequest is not null && userPassword is not null)
        {
            var passwordAttribute = (PasswordAttribute)Attribute.GetCustomAttribute(passwordPropertyRequest, typeof(PasswordAttribute))!;

            var hashedPasswordValue = (string?)userPassword.GetValue(user);
            var passwordValue = (string?)passwordPropertyRequest.GetValue(authenticationObject);
            if (passwordValue is null || hashedPasswordValue is null)
            {
                return Result<T>.Fail(AuthorizationConstants.PasswordFieldValueCantBeNull);
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
        if (registrationTokenProperty is null)
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

    public Result ValidateRegistrationToken<T>(DbContext dbContext, T authenticationObject) where T : class
    {
        if (!ServicesConfiguration.IsEmailAuthEnabled)
        {
            throw new WebFlowException(AuthorizationConstants.EmailAuthenticationMustBeEnabled);
        }

        PropertyInfo? requestRegistrationTokenProperty = FetchMappedProperty(ServicesConfiguration.EmailFieldsMap, authenticationObject, "registration_token")?[0];
        var registrationTokenValue = (Guid?)requestRegistrationTokenProperty?.GetValue(authenticationObject);
        if (requestRegistrationTokenProperty is null || registrationTokenValue is null)
        {
            return Result.Fail(AuthorizationConstants.RegistrationTokenCantBeNull);
        }

        //Absolutely not gonna bother writing a whole separate method just to allow a a single type
        T? user = FetchDatabaseObject(new List<PropertyInfo>() { requestRegistrationTokenProperty }, authenticationObject, dbContext);
        if (user is null)
        {
            return Result.Fail(AuthorizationConstants.AccountDoesntExist);
        }

        PropertyInfo? databaseRegistrationTokenProperty = FetchMappedProperty(ServicesConfiguration.EmailFieldsMap, user, "registration_token")?[0];
        var databaseRegistrationTokenValue = (Guid?)databaseRegistrationTokenProperty?.GetValue(user);

        if (databaseRegistrationTokenProperty is null || registrationTokenValue == Guid.Empty)
        {
            return Result.Fail(AuthorizationConstants.AccountAlreadyVerified);
        }

        if (databaseRegistrationTokenValue != registrationTokenValue)
        {
            return Result.Fail(AuthorizationConstants.InvalidRegistrationToken);
        }

        requestRegistrationTokenProperty.SetValue(user, Guid.Empty);
        
        return Result.Ok();
    }

    public Result<T?> UpdatePassword<T>(DbContext dbContext, T authenticationObject, string? newPassword = null) where T : class
    {
        List<PropertyInfo>? authenticationFields = FetchMappedProperty(ServicesConfiguration.AuthenticationPropertiesMap, authenticationObject, "authentication_fields");
        if (authenticationFields is null ||
            FetchDatabaseObject(authenticationFields, authenticationObject, dbContext) is not { } databaseUser)
        {
            return Result<T>.Fail(AuthorizationConstants.InvalidParameters);
        }

        PropertyInfo? databasePasswordTokenProperty = FetchMappedProperty(ServicesConfiguration.EmailFieldsMap, databaseUser, "password_token")?[0];
        if (databasePasswordTokenProperty is null)
        {
            throw new WebFlowException(AuthorizationConstants.FailedToReadType(typeof(T)));
        }

        if (newPassword is null)
        {
            string token = (DateTimeOffset.Now.ToUnixTimeSeconds() + 900) + ":" + Guid.NewGuid();
            databasePasswordTokenProperty.SetValue(databaseUser, token);
            
            return Result<T?>.Ok(databaseUser);
        }
        
        PropertyInfo? requestPasswordTokenProperty = FetchMappedProperty(ServicesConfiguration.EmailFieldsMap, authenticationObject, "password_token")?[0];
        var requestPasswordToken = (string?)requestPasswordTokenProperty?.GetValue(databaseUser);
        if (requestPasswordTokenProperty is null || requestPasswordToken is null)
        {
            throw new WebFlowException(AuthorizationConstants.FailedToReadType(typeof(T)));
        }
        
        string[] tokenParts = requestPasswordToken.Split(':');
        
        long tokenTime = long.TryParse(tokenParts[0], out var tt) ? tt : 0;
        long currentTime = DateTimeOffset.Now.ToUnixTimeSeconds();
        if (currentTime > tokenTime)
        {
            return Result<T>.Fail(AuthorizationConstants.PasswordTokenExpired);
        }
        
        PropertyInfo? databasePasswordProperty = FetchMappedProperty(ServicesConfiguration.AuthenticationPropertiesMap, databaseUser, "password")?[0];
        if (databasePasswordProperty is null)
        {
            throw new WebFlowException(AuthorizationConstants.FailedToReadType(typeof(T)));
        }
        
        var databasePasswordToken = (string?)databasePasswordTokenProperty?.GetValue(databaseUser);
        if (requestPasswordToken != databasePasswordToken)
        {
            return Result<T>.Fail(AuthorizationConstants.InvalidToken);
        }

        Result result = SetPassword(ref databaseUser);

        return !result.IsSuccess 
            ? Result<T>.Fail((string)result.Error) 
            : Result<T?>.Ok(databaseUser);
    }
}