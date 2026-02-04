namespace SmhiApi.Infrastructure.Clients.Smhi;

/// <summary>
/// Client for interacting with SMHI Open Data API
/// </summary>
public interface ISmhiClient
{
    /// <summary>
    /// Gets all stations for a specific parameter
    /// </summary>
    /// <param name="parameterId">Parameter ID (1 for air temperature, 21 for wind gust)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SMHI parameter response containing stations</returns>
    Task<SmhiParameterResponse> GetStationsAsync(int parameterId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets observations for a specific station and parameter
    /// </summary>
    /// <param name="parameterId">Parameter ID (1 for air temperature, 21 for wind gust)</param>
    /// <param name="stationId">Station ID</param>
    /// <param name="period">Period (latest-hour or latest-day)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Observation response, or null if station not found</returns>
    Task<SmhiObservationResponse?> GetStationObservationsAsync(int parameterId, string stationId, string period, CancellationToken cancellationToken);

    /// <summary>
    /// Gets observations for all stations for a specific parameter
    /// </summary>
    /// <param name="parameterId">Parameter ID (1 for air temperature, 21 for wind gust)</param>
    /// <param name="period">Period (latest-hour or latest-day)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Station set response containing all stations with observations</returns>
    Task<SmhiStationSetResponse> GetObservationAllStationsAsync(int parameterId, string period, CancellationToken cancellationToken);
}
