using Microsoft.Extensions.Logging;
using NSubstitute;
using SmhiApi.Features.Stations;
using SmhiApi.Infrastructure.Clients.Smhi;

namespace SmhiApi.Tests.Features.Stations;

public class StationsServiceTests
{
    private readonly ISmhiClient _smhiClient = Substitute.For<ISmhiClient>();
    private readonly ILogger<StationsService> _logger = Substitute.For<ILogger<StationsService>>();
    private readonly StationsService _sut;

    public StationsServiceTests()
    {
        _sut = new StationsService(_smhiClient, _logger);
    }

    [Fact]
    public async Task GetAllAsync_WhenSmhiReturnsData_ReturnsMergedStations()
    {
        // Arrange
        var temperatureStations = new SmhiParameterResponse
        {
            Station = [new SmhiStation { Key = "1", Name = "Stockholm Temp" }]
        };
        var windGustStations = new SmhiParameterResponse
        {
            Station = [new SmhiStation { Key = "2", Name = "Göteborg Wind" }]
        };

        _smhiClient.GetStationsAsync(1, Arg.Any<CancellationToken>()).Returns(temperatureStations);
        _smhiClient.GetStationsAsync(21, Arg.Any<CancellationToken>()).Returns(windGustStations);

        // Act
        var result = await _sut.GetAllAsync(CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Contains(result, s => s.StationId == "1" && s.Name == "Stockholm Temp");
        Assert.Contains(result, s => s.StationId == "2" && s.Name == "Göteborg Wind");
    }

    [Fact]
    public async Task GetAllAsync_WhenDuplicateStations_RemovesDuplicates()
    {
        // Arrange
        var temperatureStations = new SmhiParameterResponse
        {
            Station = [new SmhiStation { Key = "1", Name = "Stockholm" }]
        };
        var windGustStations = new SmhiParameterResponse
        {
            Station = [new SmhiStation { Key = "1", Name = "Stockholm" }]
        };

        _smhiClient.GetStationsAsync(1, Arg.Any<CancellationToken>()).Returns(temperatureStations);
        _smhiClient.GetStationsAsync(21, Arg.Any<CancellationToken>()).Returns(windGustStations);

        // Act
        var result = await _sut.GetAllAsync(CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("1", result[0].StationId);
    }

    [Fact]
    public async Task GetAllAsync_WhenNameConflict_PrefersTemperatureName()
    {
        // Arrange
        var temperatureStations = new SmhiParameterResponse
        {
            Station = [new SmhiStation { Key = "1", Name = "Stockholm Temp Name" }]
        };
        var windGustStations = new SmhiParameterResponse
        {
            Station = [new SmhiStation { Key = "1", Name = "Stockholm Wind Name" }]
        };

        _smhiClient.GetStationsAsync(1, Arg.Any<CancellationToken>()).Returns(temperatureStations);
        _smhiClient.GetStationsAsync(21, Arg.Any<CancellationToken>()).Returns(windGustStations);

        // Act
        var result = await _sut.GetAllAsync(CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("Stockholm Temp Name", result[0].Name);
    }

    [Fact]
    public async Task GetAllAsync_WhenEmptyInputs_ReturnsEmptyArray()
    {
        // Arrange
        _smhiClient.GetStationsAsync(1, Arg.Any<CancellationToken>()).Returns(new SmhiParameterResponse());
        _smhiClient.GetStationsAsync(21, Arg.Any<CancellationToken>()).Returns(new SmhiParameterResponse());

        // Act
        var result = await _sut.GetAllAsync(CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }
}
