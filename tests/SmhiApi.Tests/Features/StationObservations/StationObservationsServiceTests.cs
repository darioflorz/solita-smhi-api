using Microsoft.Extensions.Logging;
using NSubstitute;
using SmhiApi.Features.StationObservations;
using SmhiApi.Infrastructure.Clients.Smhi;

namespace SmhiApi.Tests.Features.StationObservations;

public class StationObservationsServiceTests
{
    private readonly ISmhiClient _smhiClient = Substitute.For<ISmhiClient>();
    private readonly ILogger<StationObservationsService> _logger = Substitute.For<ILogger<StationObservationsService>>();
    private readonly StationObservationsService _sut;

    public StationObservationsServiceTests()
    {
        _sut = new StationObservationsService(_smhiClient, _logger);
    }

    [Fact]
    public async Task GetLatestAsync_WhenObservationsExist_ReturnsMergedObservations()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var tempResponse = new SmhiStationSetResponse
        {
            Station = [new SmhiStationSetStation
            {
                Key = "1",
                Name = "Stockholm",
                Value = [new SmhiObservationValue { Date = timestamp, Value = "20.5" }]
            }]
        };
        var windResponse = new SmhiStationSetResponse
        {
            Station = [new SmhiStationSetStation
            {
                Key = "1",
                Name = "Stockholm",
                Value = [new SmhiObservationValue { Date = timestamp, Value = "8.3" }]
            }]
        };

        _smhiClient.GetObservationAllStationsAsync(1, "latest-hour", Arg.Any<CancellationToken>())
            .Returns(tempResponse);
        _smhiClient.GetObservationAllStationsAsync(21, "latest-hour", Arg.Any<CancellationToken>())
            .Returns(windResponse);

        // Act
        var result = await _sut.GetLatestAsync(CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("1", result[0].StationId);
        Assert.Equal("Stockholm", result[0].Name);
        Assert.Single(result[0].Observations);
        Assert.Equal(20.5, result[0].Observations[0].AirTemp);
        Assert.Equal(8.3, result[0].Observations[0].WindGust);
    }

    [Fact]
    public async Task GetLatestAsync_WhenNoObservations_ReturnsEmptyObservationsArray()
    {
        // Arrange
        var tempResponse = new SmhiStationSetResponse
        {
            Station = [new SmhiStationSetStation { Key = "1", Name = "Stockholm", Value = [] }]
        };
        var windResponse = new SmhiStationSetResponse
        {
            Station = [new SmhiStationSetStation { Key = "1", Name = "Stockholm", Value = [] }]
        };

        _smhiClient.GetObservationAllStationsAsync(1, "latest-hour", Arg.Any<CancellationToken>())
            .Returns(tempResponse);
        _smhiClient.GetObservationAllStationsAsync(21, "latest-hour", Arg.Any<CancellationToken>())
            .Returns(windResponse);

        // Act
        var result = await _sut.GetLatestAsync(CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("1", result[0].StationId);
        Assert.Empty(result[0].Observations);
    }

    [Fact]
    public async Task GetLatestAsync_WhenOnlyTempExists_ReturnsPartialObservation()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var tempResponse = new SmhiStationSetResponse
        {
            Station = [new SmhiStationSetStation
            {
                Key = "1",
                Name = "Stockholm",
                Value = [new SmhiObservationValue { Date = timestamp, Value = "20.5" }]
            }]
        };
        var windResponse = new SmhiStationSetResponse
        {
            Station = []
        };

        _smhiClient.GetObservationAllStationsAsync(1, "latest-hour", Arg.Any<CancellationToken>())
            .Returns(tempResponse);
        _smhiClient.GetObservationAllStationsAsync(21, "latest-hour", Arg.Any<CancellationToken>())
            .Returns(windResponse);

        // Act
        var result = await _sut.GetLatestAsync(CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Single(result[0].Observations);
        Assert.Equal(20.5, result[0].Observations[0].AirTemp);
        Assert.Null(result[0].Observations[0].WindGust);
    }
}
