using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using WebFlow.Attributes;
using WebFlow.Extensions;
using WebFlow.Middlewares.Jwt;
using WebFlow.Models;

namespace WebFlow.Authorization;

public enum AuthorizationType
{
    Jwt,
    Session
}

//ToDo password resets, add email verification
//ToDo if email verification is enabled on password resets or account confirmation needs additional 2 confirmation methods
public interface IWebFlowAuthorizationService
{
    /// <summary>
    /// Registers the user in the database, passwords are automatically hashed when provided
    /// </summary>
    /// <param name="dbContext">A db context created within the calling method</param>
    /// <param name="authenticationObject">The object that we want to register within the database</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Result<T?> RegisterUser<T>(DbContext dbContext, T authenticationObject) where T : class;
    
    /// <summary>
    /// Authenticates the user based on the provided [AuthenticationField] or and [PasswordHash] attributes
    /// </summary>
    /// <param name="dbContext">A db context created within the calling method</param>
    /// <param name="httpContext">The http-context of the caller method, used to issue authorization cookies</param>
    /// <param name="authenticationObject">The object that we want to register within the database</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<Result<T?>> AuthenticateUserAsync<T>(DbContext dbContext, HttpContext httpContext, T authenticationObject) where T : class;
    
    /// <summary>
    /// Logs the user out and invalidates their session
    /// </summary>
    /// <param name="httpContext">The http-context of the caller method</param>
    /// <returns></returns>
    Result LogoutUser(HttpContext httpContext);
}

public static partial class RegisterWebFlowServices
{
    public static void RegisterAuthorizationService(this IServiceCollection serviceCollection, Assembly executing, JwtConfig jwtConfig)
    {
        foreach (var type in executing.GetTypes())
        {
            PropertyInfo[] classProperties = type.GetProperties();
            if(classProperties.Length is 0)
                continue;
            
            var dictionary = new Dictionary<string, List<PropertyInfo>>();

            List<PropertyInfo> authenticationFields = classProperties.Where(p => p.GetCustomAttribute<AuthenticationFieldAttribute>() is not null).ToList();
            if(authenticationFields.Count is not 0)
                dictionary.Add("authentication_fields", authenticationFields);
            
            List<PropertyInfo> passwordProperty = classProperties.Where(x => x.GetCustomAttribute<PasswordAttribute>() is not null).ToList();
            if (passwordProperty.Count is > 1)
                throw new WebFlowException(AuthorizationConstants.OnePasswordAttribute);
            else if (passwordProperty.Count is not 0)
                dictionary.Add("password", passwordProperty); //Password = list[0]!

            List<PropertyInfo> uniqueAttributes = classProperties.Where(p => p.GetCustomAttribute<UniqueAttribute>() is not null).ToList();
            if(uniqueAttributes.Count is not 0)
                dictionary.Add("unique_properties", uniqueAttributes);
            
            List<PropertyInfo> authenticationClaims = classProperties.Where(p => p.GetCustomAttribute<AuthenticationClaimAttribute>() is not null).ToList();
            if(authenticationClaims.Count is not 0)
                dictionary.Add("authentication_claims", uniqueAttributes);
            
            ServicesConfiguration.AuthenticationPropertiesMap.Add(type, dictionary);
        }
        
        serviceCollection.AddSingleton(jwtConfig);
        serviceCollection.AddTransient(typeof(IWebFlowAuthorizationService), typeof(WebFlowAuthorizationImplementation));
    }

    public static void RegisterAuthorizationMiddlewares(this IApplicationBuilder applicationBuilder, AuthorizationType authorizationType)
    {
        switch (authorizationType)
        {
            case AuthorizationType.Jwt:
                applicationBuilder.UseMiddleware<JwtValidatorMiddleware>();
                break;
            
            case AuthorizationType.Session:
                break;
            
            default:
                throw new ArgumentOutOfRangeException(nameof(authorizationType), authorizationType, null);
        }
        
        ServicesConfiguration.AuthorizationType = authorizationType;
    }
    
    public static void UseEmailVerification(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            List<PropertyInfo> properties = type.GetProperties()
                .Where(p => p.GetCustomAttribute<RequiresEmailVerificationAttribute>() is not null)
                .ToList();
            
            if(properties.Count is 0)
                continue;
            
            List<PropertyInfo> emailProperty = properties.Where(x => x.GetCustomAttribute<EmailAddressAttribute>() is not null).ToList();
            if (emailProperty.Count is 0)
                throw new WebFlowException(AuthorizationConstants.ClassMustHaveEmailDefined);
            
            List<PropertyInfo> registrationToken = properties.Where(x => x.GetCustomAttribute<RegistrationTokenAttribute>() is not null).ToList();
            if (registrationToken.Count is 0)
                throw new WebFlowException(AuthorizationConstants.ClassMustHaveRegTokenDefined);
            
            var propertyDictionary = new Dictionary<string, List<PropertyInfo>>()
            {
                { "email_property", emailProperty },
                { "registration_token", registrationToken }
            };

            ServicesConfiguration.IsEmailAuthEnabled = true;
            ServicesConfiguration.EmailFieldsMap.Add(type, propertyDictionary);
        }
    }
}