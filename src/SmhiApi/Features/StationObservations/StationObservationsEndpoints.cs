using Microsoft.AspNetCore.Http.HttpResults;

namespace SmhiApi.Features.StationObservations;

public static class StationObservationsEndpoints
{
    public static void MapStationObservationsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/stationObservations", GetLatestObservations)
            .WithName("GetLatestObservations")
            .WithTags("StationObservations")
            .WithSummary("Get latest observations for all stations")
            .WithDescription("Returns the latest observation (last hour) for all stations, merged from temperature and wind gust parameters.");
    }

    private static async Task<Ok<StationObservationDto[]>> GetLatestObservations(
        IStationObservationsService stationObservationsService,
        CancellationToken cancellationToken)
    {
        var observations = await stationObservationsService.GetLatestAsync(cancellationToken);
        return TypedResults.Ok(observations);
    }
}
