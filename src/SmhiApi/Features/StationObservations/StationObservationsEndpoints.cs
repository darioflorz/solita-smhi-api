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

        app.MapGet("/api/stationObservations/{stationId}", GetObservationsByStationId)
            .WithName("GetObservationsByStationId")
            .WithTags("StationObservations")
            .WithSummary("Get observations for a specific station")
            .WithDescription("Returns observations for a specific station, merged from temperature and wind gust parameters. Use 'range' query parameter to specify time range (lastHour or lastDay).");
    }

    private static async Task<Ok<StationObservationDto[]>> GetLatestObservations(
        IStationObservationsService stationObservationsService,
        CancellationToken cancellationToken)
    {
        var observations = await stationObservationsService.GetLatestAsync(cancellationToken);
        return TypedResults.Ok(observations);
    }

    private static async Task<Results<Ok<StationObservationDto>, NotFound>> GetObservationsByStationId(
        string stationId,
        IStationObservationsService stationObservationsService,
        CancellationToken cancellationToken,
        string range = "lastHour")
    {
        var observation = await stationObservationsService.GetByStationIdAsync(stationId, range, cancellationToken);
        
        if (observation is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(observation);
    }
}
