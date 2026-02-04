namespace SmhiApi.Features.StationObservations;

public record StationObservationDto(string StationId, string Name, ObservationDto[] Observations);
