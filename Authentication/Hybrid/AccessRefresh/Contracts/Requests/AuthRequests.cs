using System.ComponentModel.DataAnnotations;
using AccessRefresh.Contracts.DTOs;

namespace AccessRefresh.Contracts.Requests;

public sealed class SignUpRequest
{
    [MinLength(3, ErrorMessage = "Username must be at least 3 characters")]
    [MaxLength(50, ErrorMessage = "Username cannot exceed 50 characters")]
    [RegularExpression("^[a-zA-Z0-9_]+$", ErrorMessage = "Username can only contain letters, numbers, and underscores")]
    public required string Username { get; set; }
    
    [EmailAddress(ErrorMessage = "Invalid email address format")]
    public required string Email { get; set; }
    
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    [MaxLength(100, ErrorMessage = "Password cannot exceed 100 characters")]
    public required string Password { get; set; }
    
    public required string CallbackUrl { get; set; }
}

public record SignInRequest(string Email, string Password);
public record ForgotPasswordRequest(string Email, string CallbackUrl);
public record ResetPasswordRequest(string NewPassword, string Token);
public record MagicLinkGenerateRequest(string Password);
public record TokenVerifyRequest(string Token);