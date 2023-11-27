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

public interface IWebFlowAuthorizationService
{
    /// <summary>
    /// Registers a user into the authentication database. 
    /// Incoming password for the user is automatically hashed prior to storage with the [Password] attribute.
    /// </summary>
    /// <param name="dbContext">Database context used for user registration operation.</param>
    /// <param name="authenticationObject">Object containing the information of user to be registered.</param>
    /// <typeparam name="T">Type of the authentication object.</typeparam>
    /// <returns>Result of the registration operation indicating success or failure along with the registered user object.</returns>
    Result<T?> RegisterUser<T>(DbContext dbContext, T authenticationObject) where T : class;
    
    /// <summary>
    /// Authenticates a user based on the provided credentials (identified with [AuthenticationField] and [Password] attributes).
    /// </summary>
    /// <param name="dbContext">Database context used for authentication process.</param>
    /// <param name="httpContext">HttpContext of the caller endpoint utilized for issuing the resulting authorization cookie.</param>
    /// <param name="authenticationObject">Object containing the credentials of the user attempting to authenticate.</param>
    /// <typeparam name="T">Type of the authentication object.</typeparam>
    /// <returns>A Task leading to a Result of the authentication operation indicating success or failure along with the authenticated user object.</returns>
    Task<Result<T?>> AuthenticateUserAsync<T>(DbContext dbContext, HttpContext httpContext, T authenticationObject) where T : class;
    
    /// <summary>
    /// Terminates the authenticated user's session, revoking their current authentication cookie.
    /// </summary>
    /// <param name="httpContext">HttpContext of the caller endpoint from which the user's cookie is extracted.</param>
    /// <returns>A Result indication the success or failure of the logout operation.</returns>
    Result LogoutUser(HttpContext httpContext);

    /// <summary>
    /// Validates a user's registration token. If valid, the corresponding user is marked as verified and allowed to log in.
    /// <para>Usable only when `UseEmailVerification` is enabled.</para>
    /// </summary>
    /// <param name="dbContext">Database context used for the validation process.</param>
    /// <param name="authenticationObject">Object containing the user's registration token to validate.</param>
    /// <typeparam name="T">Type of the authentication object.</typeparam>
    /// <returns>The Result of the validation operation indicating whether it was successful or not.</returns>
    Result ValidateRegistrationToken<T>(DbContext dbContext, T authenticationObject) where T : class;

    /// <summary>
    /// Updates the authenticated user's current password.
    /// </summary>
    /// <param name="dbContext">Database context used for password update operation.</param>
    /// <param name="authenticationObject">Object containing the authenticated user's information.</param>
    /// <param name="newPassword">New password for the authenticated user. If null.</param>
    /// <typeparam name="T">Type of the authentication object.</typeparam>
    /// <returns>A Result of the operation indicating success or failure along with the updated user object.</returns>
    Result<T?> UpdatePassword<T>(DbContext dbContext, T authenticationObject, string? newPassword = null) where T : class;
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
        
        serviceCollection.AddMemoryCache();
        
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
                throw new NotSupportedException("Not yet implemented, use JWT");
                break;
            
            default:
                throw new ArgumentOutOfRangeException(nameof(authorizationType), authorizationType, null);
        }
        
        ServicesConfiguration.AuthorizationType = authorizationType;
    }
    
    public static void UseEmailVerification(this IServiceCollection serviceCollection, Assembly assembly)
    {
        var typesWithRequiredEmailVerification = assembly.GetTypes()
            .Where(type =>
                type.GetCustomAttribute<RequiresEmailVerificationAttribute>() is not null)
            .ToList();
        
        foreach (var type in typesWithRequiredEmailVerification)
        {
            /*List<PropertyInfo> emailProperty = properties.Where(x => x.GetCustomAttribute<EmailAddressAttribute>() is not null).ToList();
            if (emailProperty.Count is 0)
                throw new WebFlowException(AuthorizationConstants.ClassMustHaveEmailDefined);*/

            var properties = type.GetProperties();
            
            List<PropertyInfo> passwordResetToken = properties.Where(x => x.GetCustomAttribute<PasswordResetTokenAttribute>() is not null).ToList();
            if (passwordResetToken.Count is 0)
                throw new WebFlowException(AuthorizationConstants.ClassMustHavePassTokenDefined); 
            
            List<PropertyInfo> registrationToken = properties.Where(x => x.GetCustomAttribute<RegistrationTokenAttribute>() is not null).ToList();
            if (registrationToken.Count is 0)
                throw new WebFlowException(AuthorizationConstants.ClassMustHaveRegTokenDefined);
            
            var propertyDictionary = new Dictionary<string, List<PropertyInfo>>()
            {
                { "password_token", passwordResetToken },
                { "registration_token", registrationToken }
            };

            ServicesConfiguration.EmailFieldsMap.Add(type, propertyDictionary);
        }
        
        ServicesConfiguration.IsEmailAuthEnabled = true;
    }
}