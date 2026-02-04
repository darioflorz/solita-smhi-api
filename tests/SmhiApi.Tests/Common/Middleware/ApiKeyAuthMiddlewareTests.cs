using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SmhiApi.Common.Middleware;

namespace SmhiApi.Tests.Common.Middleware;

public class ApiKeyAuthMiddlewareTests
{
    private readonly ApiKeyAuthMiddleware _middleware;
    private readonly IConfiguration _configuration;
    private bool _nextWasCalled;

    public ApiKeyAuthMiddlewareTests()
    {
        _nextWasCalled = false;
        _middleware = new ApiKeyAuthMiddleware(_ =>
        {
            _nextWasCalled = true;
            return Task.CompletedTask;
        });

        var configData = new Dictionary<string, string?>
        {
            { "ApiKeys:ValidKeys:0", "valid-key-1" },
            { "ApiKeys:ValidKeys:1", "valid-key-2" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    [Fact]
    public async Task InvokeAsync_WhenApiKeyMissing_Returns401Unauthorized()
    {
        // Arrange
        var context = CreateHttpContext("/stations");

        // Act
        await _middleware.InvokeAsync(context, _configuration);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        _nextWasCalled.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_WhenApiKeyInvalid_Returns403Forbidden()
    {
        // Arrange
        var context = CreateHttpContext("/stations");
        context.Request.Headers["X-API-Key"] = "invalid-key";

        // Act
        await _middleware.InvokeAsync(context, _configuration);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        _nextWasCalled.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_WhenApiKeyValid_CallsNextMiddleware()
    {
        // Arrange
        var context = CreateHttpContext("/stations");
        context.Request.Headers["X-API-Key"] = "valid-key-1";

        // Act
        await _middleware.InvokeAsync(context, _configuration);

        // Assert
        _nextWasCalled.Should().BeTrue();
    }

    [Theory]
    [InlineData("valid-key-1")]
    [InlineData("valid-key-2")]
    public async Task InvokeAsync_WhenAnyValidApiKey_CallsNextMiddleware(string apiKey)
    {
        // Arrange
        var context = CreateHttpContext("/stations");
        context.Request.Headers["X-API-Key"] = apiKey;

        // Act
        await _middleware.InvokeAsync(context, _configuration);

        // Assert
        _nextWasCalled.Should().BeTrue();
    }

    [Theory]
    [InlineData("/openapi/v1.json")]
    [InlineData("/openapi")]
    [InlineData("/scalar/v1")]
    [InlineData("/scalar")]
    public async Task InvokeAsync_WhenOpenApiOrScalarPath_SkipsAuthentication(string path)
    {
        // Arrange
        var context = CreateHttpContext(path);
        // No API key provided

        // Act
        await _middleware.InvokeAsync(context, _configuration);

        // Assert
        _nextWasCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhenNoValidKeysConfigured_Returns403Forbidden()
    {
        // Arrange
        var emptyConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var context = CreateHttpContext("/stations");
        context.Request.Headers["X-API-Key"] = "some-key";

        // Act
        await _middleware.InvokeAsync(context, emptyConfig);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        _nextWasCalled.Should().BeFalse();
    }

    private static DefaultHttpContext CreateHttpContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }
}
