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
    private readonly IMemoryCache _memoryCache;

    public JwtValidatorMiddleware(RequestDelegate next, IMemoryCache memoryCache)
    {
        _next = next;
        _memoryCache = memoryCache;
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

        await _next(httpContext);   
    }

    private bool ShouldApplyJwtValidation(HttpContext context)
    {
        Endpoint? endpoint = context.GetEndpoint();
        var authorizeAttribute = endpoint?.Metadata.GetMetadata<AuthorizeAttribute>();
        
        return authorizeAttribute is not null;
    }
}