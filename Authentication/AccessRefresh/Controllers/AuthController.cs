using AccessRefresh.Contracts.DTOs;
using AccessRefresh.Contracts.Requests;
using AccessRefresh.Data.Entities;
using AccessRefresh.Domain.Filters;
using AccessRefresh.Extensions;
using AccessRefresh.Services.Application.AuthService;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessRefresh.Controllers;

[ApiController]
[Route(Constants.RouteDefault)]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("sign-up")]
    public async Task<TokensDto> SignUp([FromBody] AuthRequest request)
    {
        var user = await authService.SignUpAsync(
            request.Username,
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

    [HttpPost("confirm-email")]
    public IActionResult ConfirmEmail()
    {
        return NoContent();
    }
    
    [HttpPost("forgot-password")]
    public IActionResult ForgotPassword()
    {
        return NoContent();
    }

    [HttpPost("reset-password")]
    public IActionResult ResetPassword()
    {
        return NoContent();
    }

    [HttpPost("sign-in")]
    public async Task<TokensDto> SignIn([FromBody] AuthRequest request)
    {
        var user = await authService.SignInAsync(
            request.Username,
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

    [HttpPost("refresh")]
    public async  Task<TokensDto> Refresh(TokensDto request)
    {
        return await authService.RefreshSessionAsync(request.AccessToken, request.RefreshToken, 
            HttpContext.Connection.RemoteIpAddress!,
            Request.Headers.UserAgent.ToString(),
            HttpContext.GetFingerprint());
    }
    
    [HttpDelete("revoke")]
    [MinRole(EUserRole.User)]
    public async Task<IActionResult> Revoke(bool all = false, Guid? sessionId = null)
    {
        if (all)
        {
            await authService.RevokeAllSessions(HttpContext.GetUser()!.Id);
            return NoContent();
        }

        var result = await authService.RevokeSession(
            sessionId ?? HttpContext.GetSession()!.SessionId,
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