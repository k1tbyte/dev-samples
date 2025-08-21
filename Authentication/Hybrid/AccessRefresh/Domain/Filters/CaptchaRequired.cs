using System.Net;
using System.Text.Json.Serialization;
using AccessRefresh.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AccessRefresh.Domain.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class CaptchaRequired(
    HttpClient httpClient,
    IConfiguration config, 
    ILogger<CaptchaRequired> logger, 
    string action) : ActionFilterAttribute
{
    private record CaptchaResponse(bool Success, string? Hostname, string? Action);
    
    private const string VerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";
    
    private async Task<bool> ValidateCaptchaAsync(string token, string remoteIp)
    {
        var parameters = new Dictionary<string, string>
        {
            { "secret", config["CaptchaSecretKey"]! },
            { "response", token },
            { "remoteip", remoteIp }
        };

        try
        {
            var response = await httpClient.PostAsync(VerifyUrl, new FormUrlEncodedContent(parameters));
            var result = await response.Content.ReadFromJsonAsync<CaptchaResponse>();
            return result is not null && result.Success && result.Hostname == config["ClientHostname"] &&
                   result.Action == action;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Captcha validation failed");
        }
        return false;
    }
    
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        #if DEBUG
        base.OnActionExecuting(context);
        return;
        #endif
        var token = context.HttpContext.Request.Headers["X-Captcha-Token"].ToString();
        if (string.IsNullOrWhiteSpace(token) ||
            !ValidateCaptchaAsync(token, context.HttpContext.Connection.RemoteIpAddress!.ToString()).Result)
        {
            throw DomainException.CaptchaChallengeFailed;
        }
        base.OnActionExecuting(context);
    }
}