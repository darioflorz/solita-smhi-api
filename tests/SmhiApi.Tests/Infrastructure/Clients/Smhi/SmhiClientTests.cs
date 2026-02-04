using System.Net;
using System.Net.Http.Json;
using SmhiApi.Infrastructure.Clients.Smhi;

namespace SmhiApi.Tests.Infrastructure.Clients.Smhi;

public class SmhiClientTests
{
    [Fact]
    public async Task GetStationsAsync_WhenSmhiReturnsData_ReturnsStations()
    {
        // Arrange
        var response = new SmhiParameterResponse
        {
            Station = new[]
            {
                new SmhiStation { Key = "123", Name = "Stockholm", Active = true },
                new SmhiStation { Key = "456", Name = "Göteborg", Active = true }
            }
        };

        var httpClient = CreateMockHttpClient(response);
        var client = new SmhiClient(httpClient);

        // Act
        var result = await client.GetStationsAsync(1, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Station.Length);
        Assert.Equal("123", result.Station[0].Key);
        Assert.Equal("Stockholm", result.Station[0].Name);
    }

    [Fact]
    public async Task GetStationsAsync_WhenSmhiReturnsEmptyList_ReturnsEmptyStations()
    {
        // Arrange
        var response = new SmhiParameterResponse
        {
            Station = Array.Empty<SmhiStation>()
        };

        var httpClient = CreateMockHttpClient(response);
        var client = new SmhiClient(httpClient);

        // Act
        var result = await client.GetStationsAsync(1, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Station);
    }

    [Fact]
    public async Task GetStationsAsync_WhenCalled_UsesCorrectEndpoint()
    {
        // Arrange
        var response = new SmhiParameterResponse { Station = Array.Empty<SmhiStation>() };
        string? requestedUri = null;

        var handler = new MockHttpMessageHandler((request, _) =>
        {
            requestedUri = request.RequestUri?.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(response)
            });
        });

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://smhi/")
        };
        var client = new SmhiClient(httpClient);

        // Act
        await client.GetStationsAsync(1, CancellationToken.None);

        // Assert
        Assert.Equal("https://smhi/api/version/latest/parameter/1.json", requestedUri);
    }

    [Fact]
    public async Task GetStationsAsync_ForWindGustParameter_UsesCorrectEndpoint()
    {
        // Arrange
        var response = new SmhiParameterResponse { Station = Array.Empty<SmhiStation>() };
        string? requestedUri = null;

        var handler = new MockHttpMessageHandler((request, _) =>
        {
            requestedUri = request.RequestUri?.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(response)
            });
        });

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://smhi/")
        };
        var client = new SmhiClient(httpClient);

        // Act
        await client.GetStationsAsync(21, CancellationToken.None);

        // Assert
        Assert.Equal("https://smhi/api/version/latest/parameter/21.json", requestedUri);
    }

    [Fact]
    public async Task GetStationObservationsAsync_WhenSmhiReturnsData_ReturnsObservations()
    {
        // Arrange
        var response = new SmhiObservationResponse
        {
            Station = new SmhiObservationStation { Key = "123", Name = "Stockholm" },
            Value = new[]
            {
                new SmhiObservationValue { Date = 1700000000000, Value = "5.2" },
                new SmhiObservationValue { Date = 1700003600000, Value = "5.5" }
            }
        };

        var httpClient = CreateMockHttpClient(response);
        var client = new SmhiClient(httpClient);

        // Act
        var result = await client.GetStationObservationsAsync(1, "123", "latest-hour", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("123", result.Station.Key);
        Assert.Equal(2, result.Value.Length);
        Assert.Equal("5.2", result.Value[0].Value);
    }

    [Fact]
    public async Task GetStationObservationsAsync_WhenCalled_UsesCorrectEndpoint()
    {
        // Arrange
        var response = new SmhiObservationResponse
        {
            Station = new SmhiObservationStation { Key = "123", Name = "Stockholm" },
            Value = Array.Empty<SmhiObservationValue>()
        };
        string? requestedUri = null;

        var handler = new MockHttpMessageHandler((request, _) =>
        {
            requestedUri = request.RequestUri?.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(response)
            });
        });

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://smhi/")
        };
        var client = new SmhiClient(httpClient);

        // Act
        await client.GetStationObservationsAsync(1, "123", "latest-hour", CancellationToken.None);

        // Assert
        Assert.Equal("https://smhi/api/version/latest/parameter/1/station/123/period/latest-hour/data.json", requestedUri);
    }

    [Fact]
    public async Task GetStationObservationsAsync_ForLatestDay_UsesCorrectEndpoint()
    {
        // Arrange
        var response = new SmhiObservationResponse
        {
            Station = new SmhiObservationStation { Key = "456", Name = "Malmö" },
            Value = Array.Empty<SmhiObservationValue>()
        };
        string? requestedUri = null;

        var handler = new MockHttpMessageHandler((request, _) =>
        {
            requestedUri = request.RequestUri?.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(response)
            });
        });

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://smhi/")
        };
        var client = new SmhiClient(httpClient);

        // Act
        await client.GetStationObservationsAsync(21, "456", "latest-day", CancellationToken.None);

        // Assert
        Assert.Equal("https://smhi/api/version/latest/parameter/21/station/456/period/latest-day/data.json", requestedUri);
    }

    [Fact]
    public async Task GetStationsAsync_WhenHttpFails_ThrowsHttpRequestException()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://smhi/")
        };
        var client = new SmhiClient(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetStationsAsync(1, CancellationToken.None));
    }

    [Fact]
    public async Task GetStationObservationsAsync_WhenStationNotFound_ReturnsNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://smhi/")
        };
        var client = new SmhiClient(httpClient);

        // Act
        var result = await client.GetStationObservationsAsync(1, "nonexistent", "latest-hour", CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    private HttpClient CreateMockHttpClient<T>(T response)
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(response)
            }));

        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://smhi/")
        };
    }
}

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

    public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return _handler(request, cancellationToken);
    }
}
