namespace AccessRefresh.Contracts.DTOs;

public sealed class TokensDto
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
}

public sealed class MagicLinkTokenDto
{
    public required string MagicLinkToken { get; init; }
    public required int LifeTimeInSeconds { get; init; }
}