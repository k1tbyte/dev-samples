using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AccessRefresh.Data.Entities;


[Table("users")]
public sealed class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }
    
    [Column("email")]
    [MaxLength(254)]
    public required string Email { get; set; } = string.Empty;
    
    [Column("username")]
    [MaxLength(32)]
    public required string Username { get; set; }
    
    [Column("password")]
    [MaxLength(128)]
    public required string PasswordHash { get; set; }
    
    [Column("role")]
    public EUserRole Role { get; set; } = EUserRole.User;

    [JsonIgnore]
    public IEnumerable<Session> Sessions { get; init; } = new List<Session>();
}

public enum EUserRole : byte 
{
    None = 0,
    User = 100,
    Admin = 200
}