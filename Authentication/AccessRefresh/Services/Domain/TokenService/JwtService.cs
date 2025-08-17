using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace AccessRefresh.Services.Domain.TokenService;

public class JwtService(IConfiguration config, JwtSecurityTokenHandler handler)
{
    private const string Algorithm = SecurityAlgorithms.HmacSha256;
    
    private readonly SigningCredentials _credentials = new(
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JwtSettings:Key"]!)), 
        Algorithm
    );

    private readonly TokenValidationParameters _validationParameters = new()
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JwtSettings:Key"]!)),
        ValidateIssuer = true,
        ValidIssuer = config["JwtSettings:Issuer"],
        ValidAlgorithms = [Algorithm],
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero // No clock skew
    };
    
     public static string GenerateRandomToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        
        return Convert.ToBase64String(randomBytes).TrimEnd('=');
    }
    
    public JwtSecurityToken? ReadToken(string token)
    {
        return handler.CanReadToken(token) ? handler.ReadJwtToken(token) : null;
    }
    
    public string GenerateAccessToken(IEnumerable<Claim> claims, DateTime expires)
    {
        var token = new JwtSecurityToken(
            claims: claims,
            issuer: config["JwtSettings:Issuer"],
            expires: expires,
            notBefore: DateTime.UtcNow,
            signingCredentials: _credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
    public ClaimsPrincipal ValidateToken(string token)
    {
        return handler.ValidateToken(token, _validationParameters, out var securityToken);
    }

    public async Task<TokenValidationResult?> ValidateTokenWithoutTime(string token, TimeSpan refreshWindow)
    {
        try
        {
            var parameters = _validationParameters.Clone();
            parameters.ValidateLifetime = false; // Disable lifetime validation for this check
            var result = await handler.ValidateTokenAsync(
                token,
                parameters
            );
            
            if(result.SecurityToken.ValidTo - refreshWindow > DateTime.UtcNow)
            {
                return null; // Token is still valid, no need to refresh
            }

            return result;
        }
        catch
        {
            return null;
        }
    }
}