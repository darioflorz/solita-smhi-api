using SmhiApi.Infrastructure.Clients.Smhi;

namespace SmhiApi.Features.Stations;

public class StationsService : IStationsService
{
    private const int TemperatureParameterId = 1;
    private const int WindGustParameterId = 21;

    private readonly ISmhiClient _smhiClient;
    private readonly ILogger<StationsService> _logger;

    public StationsService(ISmhiClient smhiClient, ILogger<StationsService> logger)
    {
        _smhiClient = smhiClient;
        _logger = logger;
    }

    public async Task<StationDto[]> GetAllAsync(CancellationToken cancellationToken)
    {
        var tasks = await Task.WhenAll(
            _smhiClient.GetStationsAsync(TemperatureParameterId, cancellationToken),
            _smhiClient.GetStationsAsync(WindGustParameterId, cancellationToken)
        );

        var temperatureResponse = tasks[0];
        var windGustResponse = tasks[1];

        return MergeStations(temperatureResponse.Station, windGustResponse.Station);
    }

    private StationDto[] MergeStations(SmhiStation[] temperatureStations, SmhiStation[] windGustStations)
    {
        // Use dictionary for O(1) lookups - prefer temperature station names
        var stationDict = new Dictionary<string, StationDto>();

        // Add temperature stations first (preferred for name)
        foreach (var station in temperatureStations)
        {
            var stationId = station.Key;
            stationDict[stationId] = new StationDto(stationId, station.Name);
        }

        // Add wind gust stations (only if not already present)
        foreach (var station in windGustStations)
        {
            var stationId = station.Key;
            if (!stationDict.ContainsKey(stationId))
            {
                stationDict[stationId] = new StationDto(stationId, station.Name);
            }
            else
            {
                // Log name mismatch for debugging
                var existingName = stationDict[stationId].Name;
                if (existingName != station.Name)
                {
                    _logger.LogDebug(
                        "Station name mismatch for {StationId}: temp={TempName}, wind={WindName}",
                        stationId, existingName, station.Name);
                }
            }
        }

        return stationDict.Values.ToArray();
    }
}
