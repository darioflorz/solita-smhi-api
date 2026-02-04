namespace SmhiApi.Features.Stations;

public interface IStationsService
{
    Task<StationDto[]> GetAllAsync(CancellationToken cancellationToken);
}
