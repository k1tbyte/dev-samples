using AccessRefresh.Contracts.DTOs;
using AccessRefresh.Contracts.Requests;
using AccessRefresh.Data.Entities;
using AccessRefresh.Domain.Exceptions;
using AccessRefresh.Domain.Filters;
using AccessRefresh.Extensions;
using AccessRefresh.Services.Application.AuthService;
using AccessRefresh.Services.Domain;
using Mapster;
using Microsoft.AspNetCore.Mvc;

namespace AccessRefresh.Controllers;

[ApiController]
[Route(Constants.RouteDefault)]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("sign-up")]
    [TypeFilter(typeof(CaptchaRequired), Arguments = [ "signup" ])]
    public async Task<IActionResult> SignUp([FromBody] SignUpRequest request)
    {
        await authService.InitiateSignUpAsync(request);
        return NoContent();
    }

    [HttpPost("confirm-email")]
    public async Task<TokensDto> ConfirmEmail(TokenVerifyRequest request)
    {
        var user = authService.FinalizeSignUpAsync(request.Token).GetAwaiter().GetResult();
        return await authService.CreateSessionAsync(
            user,
            // It cant be null in TCP connections
            HttpContext.Connection.RemoteIpAddress!,
            Request.Headers.UserAgent.ToString(),
            HttpContext.GetFingerprint()
        );
    }
    
    [HttpPost("forgot-password")]
    [TypeFilter(typeof(CaptchaRequired), Arguments = [ "forgotpassword" ])]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        await authService.InitiateResetPasswordAsync(request.Email, request.CallbackUrl);
        return NoContent();
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        await authService.ResetPasswordAsync(
            request.NewPassword,
            request.Token
        );
        
        return NoContent();
    }

    [HttpPost("sign-in")]
    [TypeFilter(typeof(CaptchaRequired), Arguments = [ "signin" ])]
    public async Task<TokensDto> SignIn([FromBody] SignInRequest request)
    {
        if (!ModelState.IsValid)
        {
            throw DomainException.InvalidCredentials;
        }
        
        var user = await authService.SignInAsync(
            request.Email,
            request.Password
        );
        
        return await authService.CreateSessionAsync(
            user,
            // It cant be null in TCP connections
            HttpContext.Connection.RemoteIpAddress!,
            Request.Headers.UserAgent.ToString(),
            HttpContext.GetFingerprint()
        );
    }
    
    [HttpPost("magic-link-generate")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MagicLinkTokenDto))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [TypeFilter(typeof(CaptchaRequired), Arguments = [ "magiclink" ])]
    [MinRole(EUserRole.User)]
    public async Task<IActionResult> GenerateMagicLink([FromBody] MagicLinkGenerateRequest generateRequest)
    {
        if (!PasswordService.VerifyPassword(generateRequest.Password, HttpContext.GetUser()!.PasswordHash))
        {
            return Unauthorized();
        }

        return Ok(await authService.CreateMagicLinkAsync(HttpContext.GetSessionId()));
    }
    
    [HttpPost("magic-link-verify")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TokensDto))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MagicLinkVerify([FromBody] TokenVerifyRequest request)
    {
        if(string.IsNullOrWhiteSpace(request.Token))
        {
            return BadRequest();
        }
        
        var owner = await authService.GetMagicLinkOwnerAsync(request.Token);
        if (owner == null)
        {
            return Unauthorized();
        }
        
        var token = await authService.CreateSessionAsync(
            owner,
            HttpContext.Connection.RemoteIpAddress!,
            Request.Headers.UserAgent.ToString(),
            HttpContext.GetFingerprint()
        );
        
        return Ok(token);
    }

    [HttpPost("refresh")]
    public async  Task<TokensDto> Refresh(TokensDto request)
    {
        return await authService.RefreshSessionAsync(request.AccessToken, request.RefreshToken, 
            HttpContext.Connection.RemoteIpAddress!,
            Request.Headers.UserAgent.ToString(),
            HttpContext.GetFingerprint());
    }
    
    [HttpDelete("revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [MinRole(EUserRole.User)]
    public async Task<IActionResult> Revoke(bool all = false, Guid? sessionId = null)
    {
        if (all)
        {
            await authService.RevokeAllSessions(HttpContext.GetUser()!.Id);
            return NoContent();
        }

        var result = await authService.RevokeSession(
            sessionId ?? HttpContext.GetSessionId(),
            HttpContext.GetUser()!.Id
        );
            
        return result ? NoContent() : NotFound();
    }

    [HttpGet("sessions")]
    [MinRole(EUserRole.User)]
    public async Task<IActionResult> GetSessions()
    {
        var sessions = await authService.GetSessions(HttpContext.GetUser()!.Id);
        return Ok(sessions.Adapt<IEnumerable<SessionDto>>());
    }
}