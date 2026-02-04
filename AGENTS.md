# AGENTS.md

## Project Overview

This project implements the **SMHI Station Observations API** as defined in [docs/PRD.md](docs/PRD.md).

A .NET REST API aggregating weather observations (air temperature, wind gust) from SMHI open data with stable contracts, caching, and API key authentication.

## Tech Stack

- **.NET 10.0** / ASP.NET Core
- **Minimal APIs** with typed results
- **Microsoft.Extensions.Http.Resilience** (Polly-based)
- **Microsoft.AspNetCore.OpenApi** for OpenAPI 3.1
- **IMemoryCache** for caching

## Architecture

**Vertical Slice Architecture** — all layers in a single API project, organized by feature folders.

### Solution Structure

```
SmhiApi.sln
├── src/SmhiApi/
│   ├── Program.cs
│   ├── appsettings.json
│   ├── Features/
│   │   ├── Stations/
│   │   │   ├── StationDto.cs
│   │   │   ├── StationsEndpoints.cs
│   │   │   ├── IStationsService.cs
│   │   │   └── StationsService.cs
│   │   └── StationObservations/
│   │       ├── ObservationDto.cs
│   │       ├── StationObservationDto.cs
│   │       ├── StationObservationsEndpoints.cs
│   │       ├── IStationObservationsService.cs
│   │       └── StationObservationsService.cs
│   ├── Infrastructure/
│   │   ├── SmhiClient/
│   │   │   ├── ISmhiClient.cs
│   │   │   ├── SmhiClient.cs
│   │   │   └── SmhiClientExtensions.cs
│   │   └── Caching/
│   │       ├── ICacheService.cs
│   │       └── MemoryCacheService.cs
│   └── Common/
│       └── Middleware/
│           └── ApiKeyAuthMiddleware.cs
└── tests/SmhiApi.Tests/
    ├── Features/
    │   ├── Stations/
    │   └── StationObservations/
    ├── Infrastructure/
    └── Common/
```

---

## Minimal API Patterns

### Required Pattern

Each feature folder exposes **ONE public extension method** for endpoint registration:

```csharp
// StationsEndpoints.cs
namespace SmhiApi.Features.Stations;

public static class StationsEndpoints
{
    public static void MapStationsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/stations", GetAllStations);
    }

    private static async Task<Ok<StationDto[]>> GetAllStations(
        IStationsService stationsService,
        CancellationToken cancellationToken)
    {
        var stations = await stationsService.GetAllAsync(cancellationToken);
        return TypedResults.Ok(stations);
    }
}
```

### Registration in Program.cs

```csharp
app.MapStationsEndpoints();
app.MapStationObservationsEndpoints();
```

### Prohibited Patterns

❌ **NO inline lambdas:**

```csharp
// WRONG
app.MapGet("/stations", async (IStationsService svc) => await svc.GetAllAsync());
```

❌ **NO parameter attributes:**

```csharp
// WRONG
private static async Task<Ok<StationDto[]>> GetStation(
    [FromServices] IStationsService service,
    [FromRoute] string stationId,
    [FromQuery] string? range)
```

❌ **NO .Produces() calls** — OpenAPI is inferred from TypedResults:

```csharp
// WRONG
app.MapGet("/stations", GetAllStations).Produces<StationDto[]>(200);
```

### Typed Results Usage

| Scenario | Return Type |
|----------|-------------|
| Success with data | `TypedResults.Ok(data)` |
| Not found | `TypedResults.NotFound()` |
| Unauthorized | `TypedResults.Unauthorized()` |
| Forbidden | `TypedResults.Problem(..., statusCode: 403)` |
| Service unavailable | `TypedResults.Problem(..., statusCode: 503)` |

Use `Results<T1, T2>` union types for handlers with multiple outcomes:

```csharp
private static async Task<Results<Ok<StationObservationDto>, NotFound>> GetByStationId(
    string stationId,
    IStationObservationsService service,
    CancellationToken cancellationToken)
{
    var result = await service.GetByIdAsync(stationId, cancellationToken);
    return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
}
```

---

## Polly Resilience

Use `Microsoft.Extensions.Http.Resilience` for SMHI API calls.

### Configuration in SmhiClientExtensions.cs

```csharp
namespace SmhiApi.Infrastructure.SmhiClient;

public static class SmhiClientExtensions
{
    public static IServiceCollection AddSmhiClient(this IServiceCollection services, IConfiguration config)
    {
        services.AddHttpClient<ISmhiClient, SmhiClient>(client =>
        {
            client.BaseAddress = new Uri("https://opendata-download-metobs.smhi.se/");
        })
        .AddStandardResilienceHandler();

        return services;
    }
}
```

### Resilience Behaviors (via AddStandardResilienceHandler)

- **Retry**: Exponential backoff for transient failures
- **Circuit Breaker**: Prevents cascade failures
- **Timeout**: Per-request and total timeout
- **Rate Limiter**: Prevents overloading upstream

### Custom Resilience (if needed)

```csharp
.AddResilienceHandler("smhi-pipeline", builder =>
{
    builder
        .AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true
        })
        .AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            SamplingDuration = TimeSpan.FromSeconds(30),
            FailureRatio = 0.5,
            MinimumThroughput = 10,
            BreakDuration = TimeSpan.FromSeconds(30)
        })
        .AddTimeout(TimeSpan.FromSeconds(10));
});
```

---

## Caching

Abstract caching behind `ICacheService` for testability.

### Interface

```csharp
namespace SmhiApi.Infrastructure.Caching;

public interface ICacheService
{
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl, CancellationToken cancellationToken = default);
    void Remove(string key);
}
```

### Implementation

Use `IMemoryCache` in `MemoryCacheService`.

### TTL Configuration (from PRD)

| Data | TTL |
|------|-----|
| Stations | 6 hours |
| Latest observations | 5 minutes |
| Station lastHour | 5 minutes |
| Station lastDay | 15 minutes |

### Cache Keys Convention

```csharp
public static class CacheKeys
{
    public const string Stations = "stations";
    public const string LatestObservations = "observations:latest";
    public static string StationObservations(string stationId, string range) => $"observations:{stationId}:{range}";
}
```

---

## TDD Requirements

**Mandatory**: Write failing test BEFORE implementation.

### Workflow

1. **Red**: Write failing test
2. **Green**: Minimal code to pass
3. **Refactor**: Clean up, test still passes

### Test Organization

Mirror source structure in `tests/SmhiApi.Tests/`:

```
tests/SmhiApi.Tests/
├── Features/
│   ├── Stations/
│   │   └── StationsServiceTests.cs
│   └── StationObservations/
│       ├── StationObservationsServiceTests.cs
│       └── ObservationMergeTests.cs
├── Infrastructure/
│   ├── SmhiClient/
│   │   └── SmhiClientTests.cs
│   └── Caching/
│       └── MemoryCacheServiceTests.cs
├── Common/
│   └── Middleware/
│       └── ApiKeyAuthMiddlewareTests.cs
└── Integration/
    └── EndpointTests.cs
```

### Required Coverage

| Component | Test Type |
|-----------|-----------|
| Observation merge logic | Unit |
| DTO mapping | Unit |
| Cache behavior | Unit |
| API key middleware | Unit + Integration |
| SMHI client | Integration (mock HTTP) |
| Endpoints | Integration |

### Test Naming Convention

`MethodName_Scenario_ExpectedResult`

```csharp
[Fact]
public async Task GetAllStations_WhenSmhiReturnsData_ReturnsMergedStations()

[Fact]
public async Task MergeObservations_WhenBothValuesNull_RemovesObservation()

[Fact]
public async Task GetByStationId_WhenStationNotFound_Returns404()
```

### Test Dependencies

- **xUnit** for test framework
- **NSubstitute** or **Moq** for mocking
- **FluentAssertions** for assertions
- **Microsoft.AspNetCore.Mvc.Testing** for integration tests

---

## API Authentication

Implement as middleware in `Common/Middleware/ApiKeyAuthMiddleware.cs`.

### Rules

- Header: `X-API-Key`
- Missing key → `401 Unauthorized`
- Invalid key → `403 Forbidden`
- Store valid keys in `appsettings.json` (secure vault out of scope)

### Configuration

```json
{
  "ApiKeys": {
    "ValidKeys": ["key1", "key2"]
  }
}
```

### Middleware Pattern

```csharp
namespace SmhiApi.Common.Middleware;

public class ApiKeyAuthMiddleware
{
    private const string ApiKeyHeader = "X-API-Key";
    private readonly RequestDelegate _next;

    public ApiKeyAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IConfiguration config)
    {
        if (!context.Request.Headers.TryGetValue(ApiKeyHeader, out var providedKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var validKeys = config.GetSection("ApiKeys:ValidKeys").Get<string[]>() ?? [];
        if (!validKeys.Contains(providedKey.ToString()))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        await _next(context);
    }
}
```

---

## Coding Conventions

### General

- **Nullable reference types**: Enabled (`<Nullable>enable</Nullable>`)
- **Implicit usings**: Enabled
- **Namespaces**: File-scoped (`namespace SmhiApi.Features.Stations;`)
- **Async**: Use `async/await` for all I/O operations
- **Cancellation**: Accept `CancellationToken` in all async methods
- **Records**: Use for DTOs (`public record StationDto(...)`)

### DTOs

DTOs are co-located with their respective features:

```csharp
// Features/Stations/StationDto.cs
namespace SmhiApi.Features.Stations;

public record StationDto(string StationId, string Name);
```

```csharp
// Features/StationObservations/ObservationDto.cs
namespace SmhiApi.Features.StationObservations;

public record ObservationDto(DateTime Timestamp, double? WindGust, double? AirTemp);
```

```csharp
// Features/StationObservations/StationObservationDto.cs
namespace SmhiApi.Features.StationObservations;

public record StationObservationDto(string StationId, string Name, ObservationDto[] Observations);
```

### Logging

Use `ILogger<T>` with structured logging:

```csharp
_logger.LogDebug("Station name mismatch for {StationId}: temp={TempName}, wind={WindName}", 
    stationId, tempName, windName);

_logger.LogWarning("Partial SMHI failure for parameter {Parameter}", parameter);

_logger.LogError(ex, "Failed to fetch data from SMHI");
```

### Error Handling

Return appropriate `TypedResults` based on scenario:

| Scenario | Response |
|----------|----------|
| Invalid stationId | `TypedResults.NotFound()` |
| Missing API key | 401 (middleware) |
| Invalid API key | 403 (middleware) |
| SMHI unavailable | `TypedResults.Problem(..., statusCode: 503)` |
| Partial SMHI failure | Return partial data with 200 |

---

## External APIs

### SMHI Open Data

Base URL: `https://opendata-download-metobs.smhi.se/`

| Parameter | Endpoint |
|-----------|----------|
| Air Temperature | `api/version/latest/parameter/1.json` |
| Wind Gust | `api/version/latest/parameter/21.json` |

### Station Data Endpoints

```
/api/version/latest/parameter/{parameterId}.json          # List all stations
/api/version/latest/parameter/{parameterId}/station/{stationId}.json  # Station details
/api/version/latest/parameter/{parameterId}/station/{stationId}/period/latest-hour/data.json
/api/version/latest/parameter/{parameterId}/station/{stationId}/period/latest-day/data.json
```

---

## OpenAPI / Swagger

Use `Microsoft.AspNetCore.OpenApi` — types are inferred from `TypedResults`.

### Program.cs Setup

```csharp
builder.Services.AddOpenApi();

// ...

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
```

### Swagger UI

Add `Scalar.AspNetCore` for modern OpenAPI UI:

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
```

---

## Business Logic Rules

### Station Merge Rules

1. Merge stations from both parameters by `stationId`
2. Remove duplicates
3. **Name conflict resolution**: Prefer temperature parameter name, fallback to wind gust

### Observation Merge Rules

1. Group observations by `stationId`
2. Merge by `timestamp`
3. Remove observation if **both** `windGust` and `airTemp` are null
4. Station must always remain (never skip stations)
5. Return empty `observations` array if no valid observations

### Collection Rules

- **Never return null collections** — use empty arrays
- Stations without observations → `observations: []`

---

## Performance Guidelines

- Use dictionary lookups for merges (`Dictionary<string, Station>`)
- Do not iterate full station catalog during observation calls
- Use only stations returned from SMHI observation payload
- Cache aggressively per TTL configuration

---

## Project Setup Commands

```bash
# Create solution
dotnet new sln -n SmhiApi

# Create API project
dotnet new webapi -n SmhiApi -o src/SmhiApi --use-minimal-apis

# Create test project
dotnet new xunit -n SmhiApi.Tests -o tests/SmhiApi.Tests

# Add projects to solution
dotnet sln add src/SmhiApi/SmhiApi.csproj
dotnet sln add tests/SmhiApi.Tests/SmhiApi.Tests.csproj

# Add reference from tests to API
dotnet add tests/SmhiApi.Tests/SmhiApi.Tests.csproj reference src/SmhiApi/SmhiApi.csproj

# Add required packages to API
dotnet add src/SmhiApi package Microsoft.Extensions.Http.Resilience
dotnet add src/SmhiApi package Microsoft.AspNetCore.OpenApi
dotnet add src/SmhiApi package Scalar.AspNetCore

# Add required packages to tests
dotnet add tests/SmhiApi.Tests package FluentAssertions
dotnet add tests/SmhiApi.Tests package NSubstitute
dotnet add tests/SmhiApi.Tests package Microsoft.AspNetCore.Mvc.Testing
```
