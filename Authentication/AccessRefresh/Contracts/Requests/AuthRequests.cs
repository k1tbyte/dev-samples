using System.ComponentModel.DataAnnotations;
using AccessRefresh.Contracts.DTOs;

namespace AccessRefresh.Contracts.Requests;

public sealed class AuthRequest
{
    [MinLength(3)]
    [MaxLength(50)]
  //  [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Username can only contain letters, numbers, and underscores.")]
    public required string Username { get; set; }
    public required string Password { get; set; }
}