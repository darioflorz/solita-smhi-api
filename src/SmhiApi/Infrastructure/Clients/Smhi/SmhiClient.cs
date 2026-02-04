using System.Net;
using System.Net.Http.Json;

namespace SmhiApi.Infrastructure.Clients.Smhi;

/// <summary>
/// HTTP client for SMHI Open Data API
/// </summary>
public class SmhiClient : ISmhiClient
{
    private readonly HttpClient _httpClient;

    public SmhiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<SmhiParameterResponse> GetStationsAsync(int parameterId, CancellationToken cancellationToken)
    {
        var endpoint = $"api/version/latest/parameter/{parameterId}.json";
        var response = await _httpClient.GetAsync(endpoint, cancellationToken);
        
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<SmhiParameterResponse>(cancellationToken);
        return result ?? new SmhiParameterResponse();
    }

    /// <inheritdoc />
    public async Task<SmhiObservationResponse?> GetStationObservationsAsync(
        int parameterId, 
        string stationId, 
        string period, 
        CancellationToken cancellationToken)
    {
        var endpoint = $"api/version/latest/parameter/{parameterId}/station/{stationId}/period/{period}/data.json";
        var response = await _httpClient.GetAsync(endpoint, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<SmhiObservationResponse>(cancellationToken);
    }
}
