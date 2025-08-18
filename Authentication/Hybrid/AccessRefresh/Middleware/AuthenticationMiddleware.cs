using AccessRefresh.Services.Application;
using AccessRefresh.Services.Application.AuthService;
using AccessRefresh.Services.Domain.TokenService;

namespace AccessRefresh.Middleware;

public class AuthenticationMiddleware(RequestDelegate next, JwtService jwtService)
{
    
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("X-Fingerprint", out var fingerprintValues) || 
            fingerprintValues.Count == 0 || 
            string.IsNullOrWhiteSpace(fingerprintValues[0]))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return; 
        }
    
        var fingerprint = fingerprintValues[0]!;
        context.Items["Fingerprint"] = fingerprint;
        
        if (context.Request.Headers.Authorization.Count > 0)
        {
            var authHeader = context.Request.Headers.Authorization[0];
            if (!string.IsNullOrEmpty(authHeader) && authHeader!.StartsWith("Bearer ", StringComparison.Ordinal))
            {
                var token = authHeader.AsSpan(7); // "Bearer ".Length = 7
            
                try
                {
                    var claims = jwtService.ValidateToken(token.ToString()).Claims;
                    var userId = -1;
                    var sessionId = Guid.Empty;
                    foreach (var claim in claims)
                    {
                        switch (claim.Type)
                        {
                            case TokenClaimTypes.Id:
                                userId = int.Parse(claim.Value);
                                context.Items[TokenClaimTypes.Id] = userId;
                                break;
                            case TokenClaimTypes.Username:
                                context.Items[TokenClaimTypes.Username] = claim.Value;
                                break;
                            case TokenClaimTypes.SessionId:
                                sessionId = Guid.Parse(claim.Value);
                                context.Items[TokenClaimTypes.SessionId] = sessionId;
                                break;
                        }
                    }
                    
                    var authService = context.RequestServices.GetRequiredService<IAuthService>();
                    if (await authService.IsSessionValid(sessionId, fingerprint))
                    {
                        var userService = context.RequestServices.GetRequiredService<UserService>();
                        var user = await userService.GetUserById(userId);
                        context.Items["user"] = user;
                    }
                }
                catch { /* ignored */ }
            }
        }
    
        await next(context);
    }
}