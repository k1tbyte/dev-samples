using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
using System.Text.Json.Serialization;

namespace AccessRefresh.Data.Entities;
    
[Table("sessions")]
public sealed class Session
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public Guid SessionId { get; set; }
    
    [MaxLength(255)]
    [Required(AllowEmptyStrings = false)]
    [Column("refresh_token")]
    public required string RefreshToken { get; set; }
    
    [MaxLength(255)]
    [Column("fingerprint")]
    public required string Fingerprint { get; set; }
    
    [Required]
    [Column("user_id")]
    public int UserId { get; set; }
    
    [JsonIgnore]
    public User Owner { get; init; }
    
    [MaxLength(512)]
    [Column("user_agent")]
    public string? UserAgent { get; set; }
    
    [Required]
    [Column("ip_address")]
    public required IPAddress IpAddress { get; set; }
    
    [MaxLength(64)]
    [Column("country")]
    public string? Country { get; set; }
    
    [MaxLength(2)]
    [Column("country_code")]
    public string? CountryCode { get; set; }
    
    [MaxLength(100)]
    [Column("city")]
    public string? City { get; set; }
    
    [MaxLength(100)]
    [Column("provider")]
    public string? Provider { get; set; }
    
    [MaxLength(100)]
    [Column("zip_code")]
    public string? ZipCode { get; set; }
    
    [Column("latitude", TypeName = "decimal(8, 6)")]
    public decimal? Latitude { get; set; }
    
    [Column("longitude", TypeName = "decimal(9, 6)")]
    public decimal? Longitude { get; set; }
    
    [Required]
    [Column("issued_at")]
    public long IssuedAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    
    [Required]
    [Column("expires_at")]
    public long ExpiresAt { get; set; }
    
    [Column("last_refresh_at")]
    public long LastRefreshAt { get; set; }
}