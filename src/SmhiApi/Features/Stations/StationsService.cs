namespace SmhiApi.Features.Stations;

public class StationsService : IStationsService
{
    public Task<StationDto[]> GetAllAsync(CancellationToken cancellationToken)
    {
        // Dummy response - will be replaced with actual SMHI API calls
        var dummyStations = new[]
        {
            new StationDto("1", "Stockholm"),
            new StationDto("2", "Göteborg"),
            new StationDto("3", "Malmö"),
            new StationDto("4", "Uppsala"),
            new StationDto("5", "Linköping")
        };

        return Task.FromResult(dummyStations);
    }
}
