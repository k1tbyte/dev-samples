using System.Net;
using System.Security.Claims;
using System.Text.Json;
using AccessRefresh.Contracts.DTOs;
using AccessRefresh.Data.Entities;
using AccessRefresh.Domain.Exceptions;
using AccessRefresh.Repositories.SessionRepository;
using AccessRefresh.Repositories.UserRepository;
using AccessRefresh.Services.Domain;
using AccessRefresh.Services.Domain.CacheService;
using AccessRefresh.Services.Domain.TokenService;
using AccessRefresh.Services.Infrastructure.GeolocationService;
using Microsoft.EntityFrameworkCore;

namespace AccessRefresh.Services.Application.AuthService;

public sealed class AuthService(
    IUserRepository userRepository, 
    ISessionRepository sessionRepository, 
    JwtService tokenService,
    IGeolocationService geolocation,
    ICacheManager cacheManager,
    ILogger<AuthService> logger) : IAuthService
{
    private const int ExpirationDays = 60;
    private const int AccessTokenExpirationMinutes = 10;
    private static readonly TimeSpan TokenReuseWindow = TimeSpan.FromSeconds(10);
    
    public async Task<User> SignUpAsync(
        string username,
        string password)
    {
        if (await userRepository.Set.AnyAsync(x => x.Username == username))
        {
            throw DomainException.UserAlreadyExists;
        }
        
        
        return await userRepository.WithAutoSaveNext().Add(new User
        {
            Username = username,
            PasswordHash = PasswordService.HashPassword(password)
        });
    }

    public async Task<User> SignInAsync(string username, string password)
    {
        var user = await userRepository.Set
            .FirstOrDefaultAsync(x => x.Username == username);

        if (user == null || !PasswordService.VerifyPassword(password, user.PasswordHash))
        {
            throw DomainException.InvalidCredentials;
        }

        return user;
    }

    public async Task<TokensDto> RefreshSessionAsync(
        string accessToken,
        string refreshToken,
        IPAddress ipAddress,
        string userAgent,
        string fingerprint)
    {
        var result = await tokenService.ValidateTokenWithoutTime(
            accessToken,
            TokenReuseWindow
        );
        
        if (result == null ||
            !result.Claims.TryGetValue(TokenClaimTypes.Id, out var userIdObj) || 
            !result.Claims.TryGetValue(TokenClaimTypes.SessionId, out var sessionIdObj) ||
            !int.TryParse(userIdObj.ToString(), out var userId) ||
            !Guid.TryParse(sessionIdObj.ToString(), out var sessionId))
        {
            throw DomainException.InvalidAuthToken;
        }
            
        var user = await userRepository.Set
            .Include(x => x.Sessions.Where(o => o.SessionId == sessionId))
            .FirstOrDefaultAsync(x => x.Id == userId);

        var session = user?.Sessions.FirstOrDefault();
            
        if (user == null || session == null ||
            session.Fingerprint != fingerprint ||
            session.ExpiresAt <= DateTimeOffset.UtcNow.ToUnixTimeSeconds()
           )
        {
            throw DomainException.InvalidAuthToken;
        }
        
        var cachedTokens = await cacheManager.GetAsync<TokensDto>($"reused_refresh:{refreshToken}");
        if (cachedTokens != null)
        {
            return cachedTokens;
        }

        if (session.RefreshToken != refreshToken)
        {
            throw DomainException.InvalidAuthToken;
        }

        var updatedSession = await _createSessionAsync(
            userId: user.Id,
            ipAddress: ipAddress,
            userAgent: userAgent,
            fingerprint: fingerprint);
            
        updatedSession.IssuedAt = session.IssuedAt;
        updatedSession.SessionId = session.SessionId;
        updatedSession.LastRefreshAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        sessionRepository.Set.Local.Remove(session);
        await sessionRepository.WithAutoSaveNext().Update(updatedSession);
        _ = cacheManager.SetStringAsync(
            $"session:{updatedSession.SessionId}",
            updatedSession.Fingerprint,
            TimeSpan.FromMinutes(AccessTokenExpirationMinutes)
        );

        var tokens = new TokensDto
        {
            AccessToken = _prepareTokenForSession(user, updatedSession),
            RefreshToken = session.RefreshToken
        };
        
        _ = cacheManager.SetAsync(
            $"reused_refresh:{refreshToken}",
            tokens,
            TokenReuseWindow
        );

        return tokens;
    }

    public async Task<int> RevokeAllSessions(int ownerId)
    {
        var sessions = sessionRepository.GetSessionIdsByUserIdAsync(ownerId);
        foreach (var sessionId in sessions)
        {
            _ = cacheManager.RemoveAsync($"session:{sessionId}");
        }
        
        return await sessionRepository.Set
            .Where(x => x.UserId == ownerId)
            .ExecuteDeleteAsync();
    }
    
    public async Task<bool> RevokeSession(Guid sessionId, int ownerId)
    {
        var cacheKey = $"session:{sessionId}";
        var deleteTask = sessionRepository.Set
            .Where(x => x.SessionId == sessionId && x.UserId == ownerId)
            .ExecuteDeleteAsync();

        var cacheTask = cacheManager.RemoveAsync(cacheKey);

        await Task.WhenAll(deleteTask, cacheTask);

        return deleteTask.Result > 0;
    }

    public async Task<IEnumerable<Session>> GetSessions(int ownerId)
    {
        return await sessionRepository.Set.Where(x => x.UserId == ownerId)
            .OrderByDescending(x => x.IssuedAt).ToArrayAsync();
    }
    
    /*
    public async Task<Session?> GetSession(Guid sessionId, int ownerId)
    {
        
        var cacheKey = $"session:{sessionId}:{ownerId}";
            
        var sessionCached = await cacheManager.GetAsync<Session>(cacheKey);
        if (sessionCached != null) 
        {
            return sessionCached;
        }

        var session = await sessionRepository.Set.FindAsync(sessionId);
        if (session != null)
        {
            await cacheManager.SetAsync(cacheKey, session);
        }

        return session;
    }
    */
    
    public async Task<bool> IsSessionValid(Guid sessionId, string fingerprint)
    {
        return await cacheManager.GetStringAsync($"session:{sessionId}") == fingerprint;
    }

    public async Task<TokensDto> CreateSessionAsync(
        User user,
        IPAddress ipAddress,
        string userAgent,
        string fingerprint)
    {
        await sessionRepository.EnsureSessionLimitAsync(user.Id);
        
        var session = await _createSessionAsync(
            userId: user.Id,
            ipAddress: ipAddress,
            userAgent: userAgent,
            fingerprint: fingerprint
        );
        
        await sessionRepository.Add(session);
        _ = cacheManager.SetStringAsync(
            $"session:{session.SessionId}",
            fingerprint,
            TimeSpan.FromMinutes(AccessTokenExpirationMinutes)
        );
        
        var tokens = new TokensDto
        {
            AccessToken = _prepareTokenForSession(user, session),
            RefreshToken = session.RefreshToken
        };
        
        await sessionRepository.SaveAsync();
        return tokens;
    }

    private async Task<Session> _createSessionAsync(
        int userId,
        IPAddress ipAddress,
        string userAgent,
        string fingerprint)
    {
        GeolocationInfo? locInfo = null;

        try
        {
            locInfo = await geolocation.GetGeolocationAsync(ipAddress);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
        }
        
        var sessionExpiration = DateTimeOffset.UtcNow.AddDays(ExpirationDays);
        var sessionId = Guid.NewGuid();
        
        return new Session
        {
            SessionId = sessionId,
            UserId = userId,
            RefreshToken = JwtService.GenerateRandomToken(),
            UserAgent = userAgent,
            Fingerprint = fingerprint,
            IpAddress = ipAddress,
            Country = locInfo?.Country,
            CountryCode = locInfo?.CountryCode,
            City = locInfo?.City,
            ZipCode = locInfo?.Zip,
            Latitude = locInfo?.Lat,
            Longitude = locInfo?.Lon,
            Provider = locInfo?.As,
            ExpiresAt = sessionExpiration.ToUnixTimeSeconds()
        };
    }
    
    private string _prepareTokenForSession(
        User user,
        Session session)
    {
        var accessToken = tokenService.GenerateAccessToken([
            new Claim(TokenClaimTypes.Id, user.Id.ToString()),
            new Claim(TokenClaimTypes.SessionId, session.SessionId.ToString()),
            new Claim(TokenClaimTypes.Username, user.Username)
        ], DateTime.UtcNow.AddMinutes(AccessTokenExpirationMinutes));
        
        return accessToken;
    }
}