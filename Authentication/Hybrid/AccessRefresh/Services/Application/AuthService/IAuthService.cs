using System.Net;
using AccessRefresh.Contracts.DTOs;
using AccessRefresh.Contracts.Requests;
using AccessRefresh.Data.Entities;

namespace AccessRefresh.Services.Application.AuthService;

public interface IAuthService
{
    Task InitiateSignUpAsync(SignUpRequest request);
    public Task<User> FinalizeSignUpAsync(string token);
    public Task ResetPasswordAsync(string newPassword, string token);
    public Task InitiateResetPasswordAsync(string email, string callbackUrl);

    Task<User> SignInAsync(string email, string password);

    Task<TokensDto> RefreshSessionAsync(string accessToken,
        string refreshToken,
        IPAddress ipAddress,
        string userAgent,
        string fingerprint);

    Task<TokensDto> CreateSessionAsync(User user,
        IPAddress ipAddress,
        string userAgent,
        string fingerprint);

    Task<int> RevokeAllSessions(int ownerId);
    Task<bool> RevokeSession(Guid sessionId, int ownerId);
    Task<IEnumerable<Session>> GetSessions(int ownerId);

    public Task<bool> IsSessionValid(Guid sessionId, string fingerprint);
    public Task<MagicLinkTokenDto> CreateMagicLinkAsync(Guid sessionId);
    public Task<User?> GetMagicLinkOwnerAsync(string token);
}