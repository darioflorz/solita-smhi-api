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

    [Fact]
    public async Task GetLatestAsync_WhenOnlyWindExists_ReturnsPartialObservation()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var tempResponse = new SmhiStationSetResponse { Station = [] };
        var windResponse = new SmhiStationSetResponse
        {
            Station = [new SmhiStationSetStation
            {
                Key = "1",
                Name = "Stockholm",
                Value = [new SmhiObservationValue { Date = timestamp, Value = "12.5" }]
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
        Assert.Single(result[0].Observations);
        Assert.Null(result[0].Observations[0].AirTemp);
        Assert.Equal(12.5, result[0].Observations[0].WindGust);
    }

    [Fact]
    public async Task GetLatestAsync_WhenMultipleStations_MergesCorrectly()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var tempResponse = new SmhiStationSetResponse
        {
            Station = [
                new SmhiStationSetStation { Key = "1", Name = "Stockholm", Value = [new SmhiObservationValue { Date = timestamp, Value = "20.5" }] },
                new SmhiStationSetStation { Key = "2", Name = "Göteborg", Value = [new SmhiObservationValue { Date = timestamp, Value = "18.0" }] }
            ]
        };
        var windResponse = new SmhiStationSetResponse
        {
            Station = [
                new SmhiStationSetStation { Key = "1", Name = "Stockholm", Value = [new SmhiObservationValue { Date = timestamp, Value = "8.3" }] },
                new SmhiStationSetStation { Key = "3", Name = "Malmö", Value = [new SmhiObservationValue { Date = timestamp, Value = "15.0" }] }
            ]
        };

        _smhiClient.GetObservationAllStationsAsync(1, "latest-hour", Arg.Any<CancellationToken>())
            .Returns(tempResponse);
        _smhiClient.GetObservationAllStationsAsync(21, "latest-hour", Arg.Any<CancellationToken>())
            .Returns(windResponse);

        // Act
        var result = await _sut.GetLatestAsync(CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Length);
        
        var stockholm = result.First(s => s.StationId == "1");
        Assert.Equal(20.5, stockholm.Observations[0].AirTemp);
        Assert.Equal(8.3, stockholm.Observations[0].WindGust);
        
        var goteborg = result.First(s => s.StationId == "2");
        Assert.Equal(18.0, goteborg.Observations[0].AirTemp);
        Assert.Null(goteborg.Observations[0].WindGust);
        
        var malmo = result.First(s => s.StationId == "3");
        Assert.Null(malmo.Observations[0].AirTemp);
        Assert.Equal(15.0, malmo.Observations[0].WindGust);
    }

    [Fact]
    public async Task GetLatestAsync_WhenMultipleTimestamps_ReturnsOnlyLatest()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var oldTimestamp = now.AddHours(-1).ToUnixTimeMilliseconds();
        var newTimestamp = now.ToUnixTimeMilliseconds();
        
        var tempResponse = new SmhiStationSetResponse
        {
            Station = [new SmhiStationSetStation
            {
                Key = "1",
                Name = "Stockholm",
                Value = [
                    new SmhiObservationValue { Date = oldTimestamp, Value = "15.0" },
                    new SmhiObservationValue { Date = newTimestamp, Value = "20.5" }
                ]
            }]
        };
        var windResponse = new SmhiStationSetResponse
        {
            Station = [new SmhiStationSetStation
            {
                Key = "1",
                Name = "Stockholm",
                Value = [
                    new SmhiObservationValue { Date = oldTimestamp, Value = "5.0" },
                    new SmhiObservationValue { Date = newTimestamp, Value = "8.3" }
                ]
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
        Assert.Single(result[0].Observations);
        Assert.Equal(20.5, result[0].Observations[0].AirTemp);
        Assert.Equal(8.3, result[0].Observations[0].WindGust);
    }

    [Fact]
    public async Task GetLatestAsync_WhenNameConflict_PrefersTempName()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var tempResponse = new SmhiStationSetResponse
        {
            Station = [new SmhiStationSetStation
            {
                Key = "1",
                Name = "Stockholm Temp",
                Value = [new SmhiObservationValue { Date = timestamp, Value = "20.5" }]
            }]
        };
        var windResponse = new SmhiStationSetResponse
        {
            Station = [new SmhiStationSetStation
            {
                Key = "1",
                Name = "Stockholm Wind",
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
        Assert.Equal("Stockholm Temp", result[0].Name);
    }

    #region GetByStationIdAsync Tests

    [Fact]
    public async Task GetByStationIdAsync_WhenStationExists_ReturnsMergedObservations()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var tempResponse = new SmhiObservationResponse
        {
            Station = new SmhiObservationStation { Key = "1", Name = "Stockholm" },
            Value = [new SmhiObservationValue { Date = timestamp, Value = "20.5" }]
        };
        var windResponse = new SmhiObservationResponse
        {
            Station = new SmhiObservationStation { Key = "1", Name = "Stockholm" },
            Value = [new SmhiObservationValue { Date = timestamp, Value = "8.3" }]
        };

        _smhiClient.GetStationObservationsAsync(1, "1", "latest-hour", Arg.Any<CancellationToken>())
            .Returns(tempResponse);
        _smhiClient.GetStationObservationsAsync(21, "1", "latest-hour", Arg.Any<CancellationToken>())
            .Returns(windResponse);

        // Act
        var result = await _sut.GetByStationIdAsync("1", "lastHour", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("1", result.StationId);
        Assert.Equal("Stockholm", result.Name);
        Assert.Single(result.Observations);
        Assert.Equal(20.5, result.Observations[0].AirTemp);
        Assert.Equal(8.3, result.Observations[0].WindGust);
    }

    [Fact]
    public async Task GetByStationIdAsync_WhenStationNotFound_ReturnsNull()
    {
        // Arrange
        _smhiClient.GetStationObservationsAsync(1, "999", "latest-hour", Arg.Any<CancellationToken>())
            .Returns((SmhiObservationResponse?)null);
        _smhiClient.GetStationObservationsAsync(21, "999", "latest-hour", Arg.Any<CancellationToken>())
            .Returns((SmhiObservationResponse?)null);

        // Act
        var result = await _sut.GetByStationIdAsync("999", "lastHour", CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByStationIdAsync_WhenLastDayRange_UsesCorrectPeriod()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var tempResponse = new SmhiObservationResponse
        {
            Station = new SmhiObservationStation { Key = "1", Name = "Stockholm" },
            Value = [new SmhiObservationValue { Date = timestamp, Value = "20.5" }]
        };

        _smhiClient.GetStationObservationsAsync(1, "1", "latest-day", Arg.Any<CancellationToken>())
            .Returns(tempResponse);
        _smhiClient.GetStationObservationsAsync(21, "1", "latest-day", Arg.Any<CancellationToken>())
            .Returns((SmhiObservationResponse?)null);

        // Act
        var result = await _sut.GetByStationIdAsync("1", "lastDay", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        await _smhiClient.Received(1).GetStationObservationsAsync(1, "1", "latest-day", Arg.Any<CancellationToken>());
        await _smhiClient.Received(1).GetStationObservationsAsync(21, "1", "latest-day", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByStationIdAsync_WhenMultipleObservations_ReturnsAllObservations()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var timestamp1 = now.AddHours(-1).ToUnixTimeMilliseconds();
        var timestamp2 = now.ToUnixTimeMilliseconds();
        
        var tempResponse = new SmhiObservationResponse
        {
            Station = new SmhiObservationStation { Key = "1", Name = "Stockholm" },
            Value = [
                new SmhiObservationValue { Date = timestamp1, Value = "18.0" },
                new SmhiObservationValue { Date = timestamp2, Value = "20.5" }
            ]
        };
        var windResponse = new SmhiObservationResponse
        {
            Station = new SmhiObservationStation { Key = "1", Name = "Stockholm" },
            Value = [
                new SmhiObservationValue { Date = timestamp1, Value = "5.0" },
                new SmhiObservationValue { Date = timestamp2, Value = "8.3" }
            ]
        };

        _smhiClient.GetStationObservationsAsync(1, "1", "latest-hour", Arg.Any<CancellationToken>())
            .Returns(tempResponse);
        _smhiClient.GetStationObservationsAsync(21, "1", "latest-hour", Arg.Any<CancellationToken>())
            .Returns(windResponse);

        // Act
        var result = await _sut.GetByStationIdAsync("1", "lastHour", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Observations.Length);
        // Should be ordered by timestamp descending
        Assert.Equal(20.5, result.Observations[0].AirTemp);
        Assert.Equal(18.0, result.Observations[1].AirTemp);
    }

    [Fact]
    public async Task GetByStationIdAsync_WhenOnlyTempExists_ReturnsStationWithTempObservations()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var tempResponse = new SmhiObservationResponse
        {
            Station = new SmhiObservationStation { Key = "1", Name = "Stockholm" },
            Value = [new SmhiObservationValue { Date = timestamp, Value = "20.5" }]
        };

        _smhiClient.GetStationObservationsAsync(1, "1", "latest-hour", Arg.Any<CancellationToken>())
            .Returns(tempResponse);
        _smhiClient.GetStationObservationsAsync(21, "1", "latest-hour", Arg.Any<CancellationToken>())
            .Returns((SmhiObservationResponse?)null);

        // Act
        var result = await _sut.GetByStationIdAsync("1", "lastHour", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Observations);
        Assert.Equal(20.5, result.Observations[0].AirTemp);
        Assert.Null(result.Observations[0].WindGust);
    }

    [Fact]
    public async Task GetByStationIdAsync_WhenNoValidObservations_ReturnsEmptyObservationsArray()
    {
        // Arrange
        var tempResponse = new SmhiObservationResponse
        {
            Station = new SmhiObservationStation { Key = "1", Name = "Stockholm" },
            Value = []
        };
        var windResponse = new SmhiObservationResponse
        {
            Station = new SmhiObservationStation { Key = "1", Name = "Stockholm" },
            Value = []
        };

        _smhiClient.GetStationObservationsAsync(1, "1", "latest-hour", Arg.Any<CancellationToken>())
            .Returns(tempResponse);
        _smhiClient.GetStationObservationsAsync(21, "1", "latest-hour", Arg.Any<CancellationToken>())
            .Returns(windResponse);

        // Act
        var result = await _sut.GetByStationIdAsync("1", "lastHour", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("1", result.StationId);
        Assert.Empty(result.Observations);
    }

    #endregion
}
