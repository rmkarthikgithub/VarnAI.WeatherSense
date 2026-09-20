# VarnAI WeatherSense — REST API Reference

## 1. Overview & Protocol Standards

The **VarnAI WeatherSense API** adheres to modern RESTful design standards:
- **Base Path**: `/api/v1`
- **Payload Format**: `application/json; charset=utf-8`
- **Date/Time Standard**: ISO 8601 UTC (e.g. `2026-09-19T06:00:00Z`)
- **Idempotency & Safe Methods**: `GET` is read-only; `POST` creates resources or runs collection jobs; `PUT` updates full entity state; `DELETE` soft-deactivates locations.

---

## 2. Authentication & Security

All API endpoints (except `/health*` and `/swagger*`) require authentication via a secret HTTP header:

```http
X-API-Key: your-secure-weathersense-api-key
```

### Configured Secret
The API key is verified by `ApiKeyAuthMiddleware` against the server configuration:
- Development default: `varnai-weathersense-dev-key-2026`
- In production, set via environment variable: `WeatherSenseSecurity__ApiKey`.

### Unauthorized Response (HTTP 401)
```json
{
  "type": "https://httpstatuses.com/401",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Invalid or missing X-API-Key header.",
  "instance": "/api/v1/locations",
  "correlationId": "8f3b2020-569d-4876-8884-6ffc6a287900",
  "timestampUtc": "2026-09-19T06:00:00.000Z"
}
```

---

## 3. Correlation Tracking & Standard Error Format

Every request through the pipeline is assigned a correlation identifier via `CorrelationIdMiddleware`:
- **Request Header**: `X-Correlation-ID` (optional, can be supplied by caller or reverse proxy).
- **Response Header**: `X-Correlation-ID` (always present in response).

### RFC 7807 ProblemDetails Specification
Validation and runtime errors return standardized ProblemDetails:
```json
{
  "type": "https://httpstatuses.com/400",
  "title": "Validation Failed",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "instance": "/api/v1/locations",
  "correlationId": "8f3b2020-569d-4876-8884-6ffc6a287900",
  "timestampUtc": "2026-09-19T06:00:00.000Z",
  "errors": {
    "Latitude": ["Latitude must be between -90 and 90 degrees."],
    "Code": ["Location code is required."]
  }
}
```

---

## 4. Endpoints Catalog

### 4.1 Weather Locations Management (`/api/v1/locations`)

#### `GET /api/v1/locations`
Retrieves all registered farm/hub locations.
- **Query Parameters**:
  - `isActive` (`bool`, optional): Filter by active status (e.g. `true` or `false`).
- **Response**: `200 OK`
```json
[
  {
    "id": 1,
    "name": "VarnAI Farm Alpha - Pollachi",
    "code": "LOC-FARM-001",
    "latitude": 10.660900,
    "longitude": 77.004800,
    "elevationMeters": 293.00,
    "timezone": "Asia/Kolkata",
    "district": "Coimbatore",
    "state": "Tamil Nadu",
    "country": "India",
    "isActive": true,
    "lastPolledAtUtc": "2026-09-19T05:00:00Z",
    "createdAtUtc": "2026-09-18T10:00:00Z",
    "updatedAtUtc": null
  }
]
```

#### `GET /api/v1/locations/{id}`
Retrieves a specific location by numeric ID.
- **Response**: `200 OK` (Location DTO) or `404 Not Found`.

#### `POST /api/v1/locations`
Creates a new farm or collection center location.
- **Request Body**:
```json
{
  "name": "VarnAI Polyhouse Beta - Emmampoondi",
  "code": "LOC-POLY-002",
  "latitude": 11.370000,
  "longitude": 77.530000,
  "elevationMeters": 245.0,
  "timezone": "Asia/Kolkata",
  "district": "Erode",
  "state": "Tamil Nadu",
  "country": "India",
  "isActive": true
}
```
- **Response**: `201 Created` with `Location` header `http://.../api/v1/locations/2`.

#### `PUT /api/v1/locations/{id}`
Updates an existing location.
- **Response**: `200 OK` (Updated Location DTO) or `404 Not Found`.

#### `DELETE /api/v1/locations/{id}`
Soft-deactivates the location (`IsActive = false`). Does not drop historical weather observations.
- **Response**: `204 No Content`.

---

### 4.2 Weather Collections Orchestration (`/api/v1/weather`)

#### `POST /api/v1/weather/collect`
Triggers an immediate weather and forecast collection run across active locations. This endpoint is invoked by **n8n** on an automated cron schedule, or by administrators on-demand.
- **Request Body**:
```json
{
  "locationIds": [],
  "triggerSource": "N8nWorkflow"
}
```
*(Passing an empty `locationIds` array polls all active locations in the database).*
- **Response**: `200 OK`
```json
{
  "executionId": 14,
  "startedAtUtc": "2026-09-19T06:00:00.123Z",
  "completedAtUtc": "2026-09-19T06:00:01.845Z",
  "durationMs": 1722,
  "status": "Succeeded",
  "triggerSource": "N8nWorkflow",
  "totalLocationsAttempted": 2,
  "successfulLocationsCount": 2,
  "failedLocationsCount": 0,
  "observationsPersistedCount": 48,
  "forecastsPersistedCount": 336,
  "errorDetails": null,
  "details": [
    {
      "locationId": 1,
      "locationName": "VarnAI Farm Alpha - Pollachi",
      "status": "Succeeded",
      "observationsPersistedCount": 24,
      "forecastsPersistedCount": 168,
      "durationMs": 850,
      "errorDetails": null
    }
  ]
}
```

#### `GET /api/v1/weather/collections`
Retrieves a paginated ledger of historical collection runs.
- **Query Parameters**:
  - `page` (`int`, default: `1`)
  - `pageSize` (`int`, default: `20`, max: `100`)
- **Response**: `200 OK` (Paginated list of execution summaries).

#### `GET /api/v1/weather/collections/{id}`
Retrieves detailed breakdown for an execution ID, including per-location timings and errors.
- **Response**: `200 OK` or `404 Not Found`.

---

### 4.3 Weather Data Queries (`/api/v1/weather/locations/{locationId}`)

#### `GET /api/v1/weather/locations/{locationId}/current`
Fetches the most recent actual weather observation recorded for the location.
- **Response**: `200 OK`
```json
{
  "locationId": 1,
  "locationName": "VarnAI Farm Alpha - Pollachi",
  "locationCode": "LOC-FARM-001",
  "observation": {
    "timestampUtc": "2026-09-19T05:00:00Z",
    "temperatureCelsius": 28.40,
    "apparentTemperatureCelsius": 31.20,
    "relativeHumidityPercent": 74.50,
    "dewPointCelsius": 23.40,
    "precipitationMm": 0.00,
    "windSpeedKmh": 12.60,
    "shortwaveRadiationWm2": 650.00,
    "soilTemperature0To7cm": 27.80,
    "soilMoisture0To7cm": 0.2850,
    "evapotranspirationMm": 0.42,
    "weatherCode": 1,
    "weatherCondition": "MainlyClear",
    "dataSource": "OpenMeteo"
  }
}
```

#### `GET /api/v1/weather/locations/{locationId}/hourly`
Returns hourly observations within a date range.
- **Query Parameters**:
  - `fromUtc` (`DateTime`, required, e.g. `2026-09-18T00:00:00Z`)
  - `toUtc` (`DateTime`, required, e.g. `2026-09-19T23:59:59Z`)
- **Response**: `200 OK` (Array of hourly observation records).

#### `GET /api/v1/weather/locations/{locationId}/forecast`
Returns the latest forecast predictions (or a specific historical forecast snapshot).
- **Query Parameters**:
  - `snapshotUtc` (`DateTime`, optional): Omit to fetch the latest available snapshot.
- **Response**: `200 OK`
```json
{
  "locationId": 1,
  "locationName": "VarnAI Farm Alpha - Pollachi",
  "forecastSnapshotUtc": "2026-09-19T05:00:00Z",
  "forecasts": [
    {
      "forecastTimestampUtc": "2026-09-19T06:00:00Z",
      "temperatureCelsius": 29.80,
      "apparentTemperatureCelsius": 33.10,
      "relativeHumidityPercent": 68.00,
      "precipitationMm": 0.00,
      "precipitationProbabilityPercent": 10.00,
      "windSpeedKmh": 14.20,
      "soilTemperature0To7cm": 29.10,
      "soilMoisture0To7cm": 0.2780,
      "evapotranspirationMm": 0.55,
      "weatherCode": 2,
      "weatherCondition": "PartlyCloudy"
    }
  ]
}
```

#### `GET /api/v1/weather/locations/{locationId}/history`
Paginated time-series queries for long-term historical records.
- **Query Parameters**: `fromUtc`, `toUtc`, `page`, `pageSize`.
- **Response**: `200 OK` (Paginated list with `totalCount`, `page`, `pageSize`, `totalPages`).

#### `GET /api/v1/weather/locations/{locationId}/summary`
Calculates aggregated environmental statistics over a specified timeframe.
- **Query Parameters**: `fromUtc`, `toUtc`.
- **Response**: `200 OK`
```json
{
  "locationId": 1,
  "locationName": "VarnAI Farm Alpha - Pollachi",
  "fromUtc": "2026-09-18T00:00:00Z",
  "toUtc": "2026-09-19T00:00:00Z",
  "averageTemperatureCelsius": 26.85,
  "minTemperatureCelsius": 22.10,
  "maxTemperatureCelsius": 33.40,
  "averageRelativeHumidityPercent": 76.20,
  "totalPrecipitationMm": 14.60,
  "averageWindSpeedKmh": 11.40,
  "maxWindGustsKmh": 28.50,
  "averageShortwaveRadiationWm2": 420.50,
  "averageSoilTemperature0To7cm": 26.30,
  "averageSoilMoisture0To7cm": 0.2940,
  "totalEvapotranspirationMm": 3.85,
  "predominantCondition": "PartlyCloudy",
  "observationCount": 24
}
```

#### `GET /api/v1/weather/locations/{locationId}/dataset`
Generates a flat, ML-ready historical matrix with rolling statistics and engineered features.
- **Query Parameters**: `fromUtc`, `toUtc`.
- **Response**: `200 OK` (Flat JSON array ready for Pandas DataFrame ingestion).

---

### 4.4 System Health Check Endpoints

- `GET /health`: Liveness probe (HTTP 200 Healthy).
- `GET /health/ready`: Readiness probe verifying SQL Server connectivity (HTTP 200 Healthy, or 503 Unhealthy).
- `GET /health/live`: Basic runtime responsive probe (HTTP 200 Healthy).
*(All health endpoints are accessible without `X-API-Key` for container orchestrator liveness checks).*
