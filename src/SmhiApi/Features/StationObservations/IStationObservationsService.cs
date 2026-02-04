namespace SmhiApi.Features.StationObservations;

public interface IStationObservationsService
{
    /// <summary>
    /// Gets the latest observations for all stations
    /// </summary>
    Task<StationObservationDto[]> GetLatestAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets observations for a specific station
    /// </summary>
    /// <param name="stationId">Station ID</param>
    /// <param name="range">Time range: "lastHour" or "lastDay"</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Station observations, or null if station not found</returns>
    Task<StationObservationDto?> GetByStationIdAsync(string stationId, string range, CancellationToken cancellationToken);
}
