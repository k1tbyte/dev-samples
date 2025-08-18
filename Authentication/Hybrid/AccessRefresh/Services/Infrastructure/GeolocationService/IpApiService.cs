using System.Net;

namespace AccessRefresh.Services.Infrastructure.GeolocationService;

public sealed class IpApiService(HttpClient httpClient) : IGeolocationService
{
    public Task<GeolocationInfo?> GetGeolocationAsync(IPAddress ipAddress)
    {
        return httpClient.GetFromJsonAsync<GeolocationInfo>(
            $"http://ip-api.com/json/{ipAddress}"
        );
    }
}