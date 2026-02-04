using SmhiApi.Infrastructure.Clients.Smhi;

namespace SmhiApi.Features.StationObservations;

public class StationObservationsService : IStationObservationsService
{
    private const int TemperatureParameterId = 1;
    private const int WindGustParameterId = 21;
    private const string LatestHourPeriod = "latest-hour";

    private readonly ISmhiClient _smhiClient;
    private readonly ILogger<StationObservationsService> _logger;

    public StationObservationsService(
        ISmhiClient smhiClient,
        ILogger<StationObservationsService> logger)
    {
        _smhiClient = smhiClient;
        _logger = logger;
    }

    public async Task<StationObservationDto[]> GetLatestAsync(CancellationToken cancellationToken)
    {
        // Fetch observations for all stations in parallel for both parameters
        var tasks = await Task.WhenAll(
            _smhiClient.GetObservationAllStationsAsync(TemperatureParameterId, LatestHourPeriod, cancellationToken),
            _smhiClient.GetObservationAllStationsAsync(WindGustParameterId, LatestHourPeriod, cancellationToken)
        );

        var tempResponse = tasks[0];
        var windResponse = tasks[1];

        return MergeStationObservations(tempResponse, windResponse);
    }

    private StationObservationDto[] MergeStationObservations(
        SmhiStationSetResponse tempResponse,
        SmhiStationSetResponse windResponse)
    {
        // Build dictionary of all stations with their observations
        var stationDict = new Dictionary<string, (string Name, Dictionary<long, (double? Temp, double? Wind)> Observations)>();

        // Process temperature stations
        foreach (var station in tempResponse.Station)
        {
            if (!stationDict.ContainsKey(station.Key))
            {
                stationDict[station.Key] = (station.Name, new Dictionary<long, (double? Temp, double? Wind)>());
            }

            foreach (var value in station.Value)
            {
                if (TryParseValue(value.Value, out var temp))
                {
                    stationDict[station.Key].Observations[value.Date] = (temp, null);
                }
            }
        }

        // Process wind gust stations and merge
        foreach (var station in windResponse.Station)
        {
            if (!stationDict.ContainsKey(station.Key))
            {
                stationDict[station.Key] = (station.Name, new Dictionary<long, (double? Temp, double? Wind)>());
            }
            else if (stationDict[station.Key].Name != station.Name)
            {
                _logger.LogDebug(
                    "Station name mismatch for {StationId}: temp={TempName}, wind={WindName}",
                    station.Key, stationDict[station.Key].Name, station.Name);
            }

            foreach (var value in station.Value)
            {
                if (TryParseValue(value.Value, out var wind))
                {
                    var observations = stationDict[station.Key].Observations;
                    if (observations.TryGetValue(value.Date, out var existing))
                    {
                        observations[value.Date] = (existing.Temp, wind);
                    }
                    else
                    {
                        observations[value.Date] = (null, wind);
                    }
                }
            }
        }

        // Convert to DTOs
        return stationDict
            .Select(kvp => CreateStationObservationDto(kvp.Key, kvp.Value.Name, kvp.Value.Observations))
            .ToArray();
    }

    private static StationObservationDto CreateStationObservationDto(
        string stationId,
        string name,
        Dictionary<long, (double? Temp, double? Wind)> observations)
    {
        // Get only the latest observation per station
        var latestObservation = observations
            .Where(kvp => kvp.Value.Temp.HasValue || kvp.Value.Wind.HasValue)
            .OrderByDescending(kvp => kvp.Key)
            .Take(1)
            .Select(kvp => new ObservationDto(
                DateTimeOffset.FromUnixTimeMilliseconds(kvp.Key).UtcDateTime,
                kvp.Value.Wind,
                kvp.Value.Temp))
            .ToArray();

        return new StationObservationDto(stationId, name, latestObservation);
    }

    private static bool TryParseValue(string? value, out double result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return double.TryParse(value, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out result);
    }
}
