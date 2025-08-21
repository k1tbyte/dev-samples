using System.Net;
using AccessRefresh.Contracts.DTOs;
using AccessRefresh.Data.Entities;

namespace AccessRefresh.Services.Application.AuthService;

public interface IAuthService
{
    Task<User> SignUpAsync(string username, string password);

    Task<User> SignInAsync(string username, string password);

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
 //   public Task<Session?> GetSession(Guid sessionId, int ownerId);
  //  public bool IsSessionValid(Session? session, string fingerprint);

  public Task<bool> IsSessionValid(Guid sessionId, string fingerprint);
  public Task<MagicLinkTokenDto> CreateMagicLinkAsync(Guid sessionId);
  public Task<User?> GetMagicLinkOwnerAsync(string token);
}