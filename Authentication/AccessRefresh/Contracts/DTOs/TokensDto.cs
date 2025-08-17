namespace AccessRefresh.Contracts.DTOs;

public sealed class TokensDto
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
}