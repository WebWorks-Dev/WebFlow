using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFlow.Caching;
using WebFlow.Authorization;
using WebFlow.Extensions;

namespace WebFlowTest.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/v1/user")]
public class AccountController : ControllerBase
{
    private readonly IWebFlowAuthorizationService _authorizationService;
    private readonly IGenericCacheService _genericCacheService;
    private readonly IDbContextFactory<EntityFrameworkContext> _dbContext;

    public AccountController(IWebFlowAuthorizationService authorizationService, IDbContextFactory<EntityFrameworkContext> dbContext, IGenericCacheService genericCacheService)
    {
        _authorizationService = authorizationService;
        _dbContext = dbContext;
        _genericCacheService = genericCacheService;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateUser(AuthorizationRequest authorizationRequest)
    {
        await using var context = await _dbContext.CreateDbContextAsync();

        Result<User?> result = _authorizationService.RegisterUser(context, (User)authorizationRequest);
        if (!result.IsSuccess)
            return BadRequest();
        
        await context.SaveChangesAsync();
        
        var cachedUser = (CachedUser)result.Unwrap()!;
        _genericCacheService.CacheObject(cachedUser);
        
        return Ok(result.Unwrap());
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginUser(AuthorizationRequest authorizationRequest)
    {
        await using var context = await _dbContext.CreateDbContextAsync();

        Result<User?> result = await _authorizationService.AuthenticateUserAsync(context, HttpContext, (User)authorizationRequest);
        if (!result.IsSuccess)
            return BadRequest();
        
        await context.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("fetch/{userId:guid}")]
    public async Task<IActionResult> FetchUser(Guid userId)
    {
        return Ok(_genericCacheService.FetchObject(typeof(CachedUser), userId.ToString()));
    }
    
    [HttpPost("fetch-all")]
    public async Task<IActionResult> FetchAll()
    {
        return Ok(_genericCacheService.FetchAll(typeof(CachedUser)));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> LoginUser()
    {
        _authorizationService.LogoutUser(HttpContext);

        return Ok();
    }
}