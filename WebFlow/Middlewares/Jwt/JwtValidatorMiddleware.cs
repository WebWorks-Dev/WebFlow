using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WebFlow.Models;

namespace WebFlow.Middlewares.Jwt;

public class JwtValidatorMiddleware
{
    private readonly RequestDelegate _next;
    private readonly JwtConfig _jwtConfig;
    private readonly IMemoryCache _memoryCache;

    public JwtValidatorMiddleware(RequestDelegate next, IMemoryCache memoryCache, JwtConfig jwtConfig)
    {
        _next = next;
        _memoryCache = memoryCache;
        _jwtConfig = jwtConfig;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        if (!ShouldApplyJwtValidation(httpContext))
        {
            await _next(httpContext);
            return;
        }

        string? jwtToken = httpContext.Request.Cookies["JwtToken"];
        string? sessionId = httpContext.Request.Cookies["WebFlowSessionId"];

        if (string.IsNullOrEmpty(jwtToken)  ||
            string.IsNullOrEmpty(sessionId) || _memoryCache.TryGetValue(sessionId, out _))
        {
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Key));

        try
        {
            tokenHandler.ValidateToken(jwtToken, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = _jwtConfig.Issuer,
                ValidAudience = _jwtConfig.Audience,
                IssuerSigningKey = key
            }, out SecurityToken validatedToken);

            await _next(httpContext);
        }
        catch (Exception)
        {
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
        }
    }

    private bool ShouldApplyJwtValidation(HttpContext context)
    {
        var endpoint = context.GetEndpoint();

        var authorizeAttribute = endpoint?.Metadata.GetMetadata<AuthorizeAttribute>();
        return authorizeAttribute != null;
    }
}