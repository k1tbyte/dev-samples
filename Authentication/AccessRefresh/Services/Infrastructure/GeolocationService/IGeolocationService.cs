using System.Net;

namespace AccessRefresh.Services.Infrastructure.GeolocationService;

public class GeolocationInfo
{
    public string? Country { get; init; }
    public string? CountryCode { get; init; }
    public string? Region { get; init; }
    public string? RegionName { get; init; }
    public string? City { get; init; }
    public string? Zip { get; init; }
    public decimal Lat { get; init; }
    public decimal Lon { get; init; }
    public string? Timezone { get; init; }
    public string? Isp { get; init; }
    public string? Org { get; init; }
    public string? As { get; init; }
}

public interface IGeolocationService
{
    /// <summary>
    /// Gets the geolocation information for a given IP address.
    /// </summary>
    /// <param name="ipAddress">The IP address to get the geolocation for.</param>
    /// <returns>A task that represents the asynchronous operation, containing the geolocation information.</returns>
    Task<GeolocationInfo?> GetGeolocationAsync(IPAddress ipAddress);
}