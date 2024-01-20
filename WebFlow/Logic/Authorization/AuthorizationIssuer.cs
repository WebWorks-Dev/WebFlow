using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using WebFlow.Attributes;
using WebFlow.Models;

using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace WebFlow.Authorization;

internal partial class WebFlowAuthorizationImplementation 
{
    private static List<Claim> CreateJwtClaims<T>(T authenticationObject, List<PropertyInfo> claimsProperties)
    {
        DateTime currentTime = DateTime.UtcNow;
        long iat = (long)(currentTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;

        var claims = new List<Claim>(claimsProperties.Capacity)
        {
            new Claim(JwtRegisteredClaimNames.Sub, "JWTServiceAccessToken"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, iat.ToString(), ClaimValueTypes.Integer),
        };
        
        claims.AddRange(from claimsProperty in claimsProperties 
        let authenticationClaimAttribute = (AuthenticationClaimAttribute)Attribute.GetCustomAttribute(claimsProperty, typeof(AuthenticationClaimAttribute))!
        let claimValue = claimsProperty.GetValue(authenticationObject).ToString() ?? ""
        
            select new Claim(authenticationClaimAttribute.ClaimName, claimValue));

        return claims;
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];

        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        
        return Convert.ToBase64String(randomNumber);
    }

    private static void ClearCookies(HttpContext httpContext)
    {
        httpContext.Response.Cookies.Delete("WebFlowSessionId");
        httpContext.Response.Cookies.Delete("JwtToken");
        httpContext.Response.Cookies.Delete("RefreshToken");
    }
    
    private void IssueAuthorizationClaims<T>(HttpContext httpContext, T authenticationObject)
    {
        List<PropertyInfo>? claimsProperties = FetchMappedProperty(ServicesConfiguration.AuthenticationPropertiesMap, authenticationObject, "authentication_claims");
        if (claimsProperties is null || claimsProperties.Capacity is 0)
            return;
        
        switch (ServicesConfiguration.AuthorizationType)
        {
            case AuthorizationType.Jwt:
            {
                /* ToDo move to reissue
                 if (httpContext.Request.Cookies["RefreshToken"] is null)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }*/

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Key));
                var signIn = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                List<Claim> claims = CreateJwtClaims(authenticationObject, claimsProperties);
                
                var token = new JwtSecurityToken(
                    _jwtConfig.Issuer,
                    _jwtConfig.Audience,
                    claims,
                    expires: _jwtConfig.Duration,
                    signingCredentials: signIn);

                var options = new CookieOptions
                {
                    HttpOnly = true, 
                };

                ClearCookies(httpContext);
    
                httpContext.Response.Cookies.Append("WebFlowSessionId", Guid.NewGuid().ToString());
                httpContext.Response.Cookies.Append("JwtToken", new JwtSecurityTokenHandler().WriteToken(token), options);
                httpContext.Response.Cookies.Append("RefreshToken", GenerateRefreshToken(), options);
                break;
            }

            case AuthorizationType.Session:
            {
                foreach (var claimsProperty in claimsProperties)
                {
                    var authenticationClaimAttribute = (AuthenticationClaimAttribute)Attribute.GetCustomAttribute(claimsProperty, typeof(AuthenticationClaimAttribute))!;
                    var claimValue = (string?)claimsProperty.GetValue(authenticationObject) ?? "";

                    httpContext.Session.SetString(authenticationClaimAttribute.ClaimName, claimValue);
                }

                httpContext.Session.SetString("WebFlowSessionId", $"{Guid.NewGuid()}");
                
                httpContext.Session.CommitAsync().Wait();

                break;
            }

            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}