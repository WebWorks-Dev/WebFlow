using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using WebFlow.Helpers;
using WebFlow.Models;

namespace WebFlow.Attributes;

//Credit: https://github.com/Futuree33/asp-forums/blob/main/Data/Attributes/Recaptcha.cs

/// <summary>
/// Attribute that enables ReCaptcha verification on a method
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class RecaptchaAttribute : ActionFilterAttribute
{
    private record RecaptchaResponse([property: JsonPropertyName("success")] bool Success);

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    { 
        string reCaptchaKey = ServicesConfiguration.ReCaptchaKey 
                              ?? throw new WebFlowException(AuthorizationConstants.RecaptchaMustBeEnabled);
        
        StringValues recaptchaToken = context.HttpContext.Request.Query["recaptchaToken"];
        if (recaptchaToken.Count is 0)
        {
            context.Result = new UnauthorizedResult();
            return;
        }
        
        using var httpClient = ((IHttpClientFactory)context.HttpContext.RequestServices.GetService(typeof(IHttpClientFactory))!
                                ?? throw new WebFlowException(AuthorizationConstants.RecaptchaMustBeEnabled))
            .CreateClient("ReCaptcha");
        
        string response = await httpClient.GetStringAsync($"recaptcha/api/siteverify?secret={reCaptchaKey}&response={recaptchaToken}");
        JsonHelper.TryDeserialize<RecaptchaResponse>(response, out var reCaptchaResponse);
        
        if (reCaptchaResponse is null or { Success: false })
        {
            context.Result = new UnauthorizedResult();
            return;
        }
        
        await base.OnActionExecutionAsync(context, next);
    }
}