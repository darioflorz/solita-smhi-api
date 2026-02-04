Good additions. Those are **important architectural constraints** and they should be explicit because they affect implementation strategy, test structure, and security model.

Below is your PRD updated with those sections integrated cleanly and written so an AI agent or developer can implement against it without interpretation gaps.

---

# Product Requirements Document (PRD)

## SMHI Station Observations API

---

# 1. Overview

## 1.1 Purpose

Provide a stable REST API that aggregates weather observations from SMHI open data and exposes them in a predictable and client-friendly structure.

The API focuses on:

* Air temperature
* Wind gust

The service normalizes SMHI inconsistencies and provides:

* Stable DTO contracts
* Deterministic station handling
* Predictable null/empty behavior
* Cached responses for performance

---

# 2. Goals

## 2.1 Functional Goals

1. Provide observations grouped by station.
2. Always return stations deterministically.
3. Avoid skipping stations when observations are missing.
4. Provide station catalog endpoint for client filtering.
5. Normalize SMHI data differences across parameters.
6. Validate every request using API key authentication.
7. Provide discoverability via OpenAPI/Swagger.

---

## 2.2 Non-Functional Goals

* Fast response times using caching.
* Deterministic and consistent response shapes.
* Minimal transformation complexity.
* Maintainable code with high test coverage.
* Fully test-driven implementation.

---

# 3. Engineering Constraints

---

## 3.1 Development Methodology — TDD

The solution **must be implemented using Test Driven Development**.

### Mandatory Rules

1. Write failing test before implementation.
2. Implement minimal code to pass test.
3. Refactor only after test passes.
4. Maintain high unit test coverage for:

   * Merge logic
   * Mapping logic
   * Filtering logic
   * Cache behavior
   * Authentication middleware
   * Controller behavior

### Testing Scope

| Component      | Required Test Type      |
| -------------- | ----------------------- |
| Merge services | Unit                    |
| DTO mapping    | Unit                    |
| SMHI client    | Integration (mock HTTP) |
| Controllers    | Integration             |
| Authentication | Unit + Integration      |
| Cache layer    | Unit                    |

---

# 4. Security Requirements

## 4.1 API Key Authentication

### Requirement

Every API request must be validated using an API key.

### Rules

* API key is passed via HTTP header:

```
X-API-Key
```

### Behavior

| Condition   | Response         |
| ----------- | ---------------- |
| Missing key | 401 Unauthorized |
| Invalid key | 403 Forbidden    |
| Valid key   | Continue request |

### Storage Requirements

* Keys may be stored in configuration or memory.
* Secure key storage is **out of scope**.

---

# 5. Discoverability Requirements

## 5.1 OpenAPI / Swagger

The API must expose:

* OpenAPI 3.1 specification
* Swagger UI endpoint

### Requirements

* All endpoints documented
* All DTOs documented
* Authentication documented
* Query parameters documented
* Response codes documented
* Example responses included

---

# 6. Data Sources

## SMHI Open Data API

### Air Temperature

```
parameter/1
```

### Wind Gust

```
parameter/21
```

---

# 7. Domain Concepts

## Station

Measurement location defined by SMHI.

## Observation

Timestamped measurement value.

---

# 8. DTO Definitions

## StationDto

```json
{
  "stationId": "string",
  "name": "string"
}
```

---

## ObservationDto

```json
{
  "timestamp": "datetime",
  "windGust": "number | null",
  "airTemp": "number | null"
}
```

---

## StationObservationDto

```json
{
  "stationId": "string",
  "name": "string",
  "observations": [ObservationDto]
}
```

---

# 9. API Endpoints

---

## 9.1 GET /stations

### Description

Returns all available stations.

### Behavior

* Call SMHI station resources for temperature and wind gust.
* Merge results.
* Remove duplicates by stationId.

### Station Name Conflict Resolution

* Prefer temperature parameter name.
* Fallback to wind gust name.
* Log mismatch at debug level.

### Response

```
200 OK
StationDto[]
```

### Cache TTL

6–24 hours absolute expiration.

---

## 9.2 GET /stationObservations

### Description

Returns latest observation for all stations.

### Default Period

```
latest-hour
```

### Behavior

* Fetch both parameters.
* Merge observations.
* Return exactly one observation per station.
* Stations without data → empty observations array.

### Response

```
200 OK
StationObservationDto[]
```

### Cache TTL

1–5 minutes absolute expiration.

---

## 9.3 GET /stationObservations/{stationId}

### Query Parameters

| Parameter | Description |
| --------- | ----------- |
| range     | Optional    |

### Supported Ranges

* lastHour
* lastDay

---

### Behavior

* Validate station existence.
* Fetch observations for range.
* Merge by timestamp.
* Remove observations where both values are null.
* Never remove station.

---

### Responses

#### Station exists

```
200 OK
StationObservationDto
```

#### No valid observations

```
observations: []
```

#### Unknown station

```
404 Not Found
```

---

### Cache TTL

| Range    | TTL          |
| -------- | ------------ |
| lastHour | 1–5 minutes  |
| lastDay  | 5–15 minutes |

---

# 10. Observation Merge Rules

1. Group by stationId.
2. Merge by timestamp.
3. Remove observation if both parameters are null.
4. Station must always remain.

---

# 11. Null Handling Rules

| Condition              | Result             |
| ---------------------- | ------------------ |
| One parameter missing  | Return null value  |
| Both missing           | Remove observation |
| No observations remain | Return empty list  |

---

# 12. Error Handling

| Scenario             | Response               |
| -------------------- | ---------------------- |
| Invalid stationId    | 404                    |
| Missing API key      | 401                    |
| Invalid API key      | 403                    |
| SMHI unavailable     | 503 or cached fallback |
| Partial SMHI failure | Return partial data    |

---

# 13. Caching Strategy

## Cache Type

* Absolute expiration only.

## Cache Targets

| Endpoint            | TTL          |
| ------------------- | ------------ |
| Stations            | 6–24 hours   |
| Latest Observations | 1–5 minutes  |
| Station lastHour    | 1–5 minutes  |
| Station lastDay     | 5–15 minutes |

---

# 14. Performance Requirements

* Do not iterate over full station catalog during observation calls.
* Use only stations returned from SMHI observation payload.
* Use dictionary lookups for merges.

---

# 15. Logging Requirements

### Debug

* Station name mismatches.

### Warning

* Partial SMHI failures.

### Error

* Full upstream failure.

---

# 16. Shortcuts and Trade-offs

The following deliberate decisions simplify implementation:

---

### 16.1 Station Name Consistency

Trade-off:

* Trust SMHI names per parameter.
* Prefer temperature name.
* Accept potential mismatch.

Rationale:

* Complexity of normalization is high.
* Real impact is low.

---

### 16.2 API Key Storage

Trade-off:

* Keys stored in configuration.
* No secure vault integration.

Rationale:

* Security infrastructure is outside project scope.

---

### 16.3 Station Catalog Construction

Trade-off:

* Build catalog by merging parameter station lists.

Rationale:

* SMHI lacks unified station endpoint.

---

### 16.4 Caching Simplicity

Trade-off:

* Absolute expiration only.
* No dynamic TTL based on SMHI timestamps.

Rationale:

* Predictable cache behavior.
* Simpler implementation.

---

### 16.5 Observation Retention Rule

Trade-off:

* Remove observation entries with no valid data.

Rationale:

* Reduces client filtering logic.

---

# 17. Extensibility Considerations

New parameters must:

* Extend ObservationDto.
* Reuse merge strategy.
* Maintain endpoint contracts.

---

# 18. Consistency Principles

The API must:

1. Always return StationObservationDto objects.
2. Never return null collections.
3. Use empty arrays for missing data.
4. Maintain stable response structure.

---

# 19. Acceptance Criteria

### /stations

* Unique station list
* Deterministic naming
* Cached response

### /stationObservations

* Returns all stations present in SMHI payload
* Exactly one observation per station
* Stations never skipped

### /stationObservations/{stationId}

* Returns merged time series
* Filters invalid observations
* Returns empty observations when needed
* Returns 404 for invalid stationId

### Authentication

* All endpoints require valid API key

### Documentation

* OpenAPI spec generated
* Swagger UI enabled

---

# 20. Success Metrics

* Response time under 500 ms when cached
* Stable DTO contracts
* Full unit test coverage for business logic
* Minimal client-side filtering required
