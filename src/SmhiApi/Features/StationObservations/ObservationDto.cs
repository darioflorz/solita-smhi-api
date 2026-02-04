namespace SmhiApi.Features.StationObservations;

public record ObservationDto(DateTime Timestamp, double? WindGust, double? AirTemp);
