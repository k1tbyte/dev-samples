using System.ComponentModel.DataAnnotations;
using AccessRefresh.Contracts.DTOs;

namespace AccessRefresh.Contracts.Requests;

public sealed class AuthRequest
{
    [MinLength(3, ErrorMessage = "Username must be at least 3 characters")]
    [MaxLength(50, ErrorMessage = "Username cannot exceed 50 characters")]
    [RegularExpression("^[a-zA-Z0-9_]+$", ErrorMessage = "Username can only contain letters, numbers, and underscores")]
    public required string Username { get; set; }
    
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    [MaxLength(100, ErrorMessage = "Password cannot exceed 100 characters")]
    public required string Password { get; set; }
}

public record MagicLinkGenerateRequest(string Password);
public record MagicLinkVerifyRequest(string Token);