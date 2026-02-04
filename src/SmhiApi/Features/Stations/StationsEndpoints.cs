using Microsoft.AspNetCore.Http.HttpResults;

namespace SmhiApi.Features.Stations;

public static class StationsEndpoints
{
    public static void MapStationsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/stations", GetAllStations)
            .WithName("GetAllStations")
            .WithTags("Stations")
            .WithSummary("Get all available weather stations")
            .WithDescription("Returns all available stations merged from temperature and wind gust parameters, deduplicated by stationId.");
    }

    private static async Task<Ok<StationDto[]>> GetAllStations(
        IStationsService stationsService,
        CancellationToken cancellationToken)
    {
        var stations = await stationsService.GetAllAsync(cancellationToken);
        return TypedResults.Ok(stations);
    }
}
