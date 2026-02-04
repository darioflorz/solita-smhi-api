namespace SmhiApi.Features.StationObservations;

public interface IStationObservationsService
{
    /// <summary>
    /// Gets the latest observations for all stations
    /// </summary>
    Task<StationObservationDto[]> GetLatestAsync(CancellationToken cancellationToken);
}
