using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MimeKit;
using WebFlow.Attributes;
using WebFlow.Caching;
using WebFlow.Authorization;
using WebFlow.Email;
using WebFlow.Extensions;
using WebFlow.Helpers;
using WebFlowTest.Models;
using WebFlowTest.Templates;

namespace WebFlowTest.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/v1/user")]
public class AccountController : ControllerBase
{
    private readonly MailboxAddress _senderAddress = new MailboxAddress("YOUR_NAME", "YOUR_EMAIL");
    private readonly IWebFlowAuthorizationService _authorizationService;
    private readonly IDbContextFactory<EntityFrameworkContext> _dbContext;
    private readonly IGenericCacheService _genericCacheService;
    //private readonly IEmailService _emailService;

    public AccountController(IWebFlowAuthorizationService authorizationService,
        IDbContextFactory<EntityFrameworkContext> dbContext,
        IGenericCacheService genericCacheService)
    {
        _authorizationService = authorizationService;
        _dbContext = dbContext;
        _genericCacheService = genericCacheService;
    }

    /*[HttpPost("create")]
    public async Task<IActionResult> CreateUser(AuthorizationRequest authorizationRequest)
    {
        await using var context = await _dbContext.CreateDbContextAsync();

        Result<User?> result = _authorizationService.RegisterUser(context, (User)authorizationRequest);
        if (!result.IsSuccess)
            return BadRequest(result.Error);
        
        await context.SaveChangesAsync();
        
        User user = result.Unwrap()!;
        _genericCacheService.CacheObject((CachedUser)user);
        
        string htmlContent = await System.IO.File.ReadAllTextAsync("./Templates/Models/SignUp.html");
        var signUpTemplate = new SignUpTemplate(user.EmailAddress, user.RegistrationToken.ToString());

        await _emailService.SendOutEmailAsync(_senderAddress, authorizationRequest.EmailAddress, "Verify your email", signUpTemplate, htmlContent);
        
        return Ok(result.Unwrap());
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginUser(AuthorizationRequest authorizationRequest)
    {
        await using var context = await _dbContext.CreateDbContextAsync();

        Result<User?> result = await _authorizationService.AuthenticateUserAsync(context, HttpContext, (User)authorizationRequest);
        if (!result.IsSuccess)
            return BadRequest(result.Error);
        
        await context.SaveChangesAsync();

        return Ok();
    }
    
    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        _authorizationService.LogoutUser(HttpContext);
        
        return Ok();
    }
    
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateAccount(RegisterValidationRequest registerValidationRequest)
    {
        await using var context = await _dbContext.CreateDbContextAsync();

        Result result = _authorizationService.ValidateRegistrationToken(context, (User)registerValidationRequest);
        if (!result.IsSuccess)
            return BadRequest(result.Error);
        
        await context.SaveChangesAsync();

        return Ok();
    }
    
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest? registerValidationRequest = null, string? newPassword = null)
    {
        await using var context = await _dbContext.CreateDbContextAsync();

        Result<User?> result = _authorizationService.UpdatePassword(context, (User)registerValidationRequest, newPassword);
        if (!result.IsSuccess)
            return BadRequest(result.Error);
        
        await context.SaveChangesAsync();

        var user = result.Unwrap()!;
        if (user.PasswordResetToken is null)
            return Ok(user);
        
        string htmlContent = await System.IO.File.ReadAllTextAsync("./Templates/Models/SignUp.html");
        var signUpTemplate = new SignUpTemplate(user.EmailAddress, user.PasswordResetToken);
            
        await _emailService.SendOutEmailAsync(_senderAddress, user.EmailAddress, "Reset password", signUpTemplate, htmlContent);

        return Ok(user);
    }*/

    [HttpGet("fetch/{userId:guid}")]
    public IActionResult FetchUser(Guid userId)
    {
        return Ok(_genericCacheService.FetchObject(typeof(CachedUser), userId.ToString()));
    }
    
    [Recaptcha]
    [HttpGet("fetch-all")]
    public IActionResult FetchAll(string recaptchaToken)
    {
        return Ok(_genericCacheService.FetchAll(typeof(CachedUser)));
    }
    
    [HttpGet("fetch-all-objects")]
    public IActionResult FetchAllObjects()
    {
        var allJson = _genericCacheService.FetchAll(typeof(DbObject));
        
        var dbObjects = new List<DbObject>();
        foreach (var entryJson in allJson)
        {
            JsonHelper.TryDeserialize<DbObject>(entryJson, out var dbObject);
            if (dbObject is not null)
                dbObjects.Add(dbObject);
        }
        
        return Ok(dbObjects.OrderBy(x=>x.Index));
    }
    
    [HttpPost("create-objects")]
    public async Task<IActionResult> CreateObjects()
    {
        await using var context = await _dbContext.CreateDbContextAsync();

        for (int i = 0; i < 100; i++)
        {
            var dbObject = new DbObject() { Id =Guid.NewGuid(), Index=i};
            context.DbObject.Add(dbObject);
            _genericCacheService.CacheObject(dbObject);
        }
        
        await context.SaveChangesAsync();
        
        return Ok();
    }
    
    [HttpPost("refresh-objects-cache")]
    public async Task<IActionResult> RefreshObjects()
    {
        await using var context = await _dbContext.CreateDbContextAsync();
        var objectsList = context.DbObject.AsNoTracking().ToList();

        await _genericCacheService.RefreshCacheAsync(objectsList);
        
        return Ok();
    }
    
    [Authorize]
    [HttpGet("auth-test")]
    public IActionResult AuthTest()
    {
        return Ok();
    }
}