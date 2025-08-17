using System.Net;

namespace AccessRefresh.Contracts.DTOs;

public record SessionDto(
    Guid SessionId,
    int UserId,
    string IpAddress,
    string? Country,
    string? CountryCode,
    string? City,
    string? Provider,
    string? ZipCode,
    decimal? Latitude,
    decimal? Longitude,
    long IssuedAt,
    long ExpiresAt,
    long LastRefreshAt
);