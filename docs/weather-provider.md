# VarnAI WeatherSense — Weather Provider & Meteorological Mappings

## 1. Provider Abstraction (`IWeatherProvider`)

To ensure loose coupling and future extensibility, the Application layer interacts with meteorological services through an abstraction contract:

```csharp
public interface IWeatherProvider
{
    string ProviderName { get; }
    Task<WeatherProviderResponse> FetchWeatherDataAsync(
        decimal latitude, 
        decimal longitude, 
        string timezone, 
        CancellationToken cancellationToken = default);
}
```

This design allows VarnAI to seamlessly switch or combine providers (e.g. Open-Meteo, India Meteorological Department (IMD), Tomorrow.io, or on-farm LoRaWAN weather station hardware) without altering business logic or database schemas.

---

## 2. Open-Meteo Implementation (`OpenMeteoWeatherProvider`)

### 2.1 API Endpoint & Configuration
- **Base Endpoint**: `https://api.open-meteo.com/v1/forecast`
- **Query Method**: `GET`
- **Time Window**: 1 past day (`past_days=1`) to capture preceding actual observations + 7 forecast days (`forecast_days=7`).
- **HTTP Client**: Configured via `IHttpClientFactory` with `AddStandardResilienceHandler()` (Polly).

### 2.2 Ingested Meteorological Metrics

| Open-Meteo Variable | Unit | Target Database Column | Precision | Notes / Agricultural Usage |
| :--- | :--- | :--- | :--- | :--- |
| `temperature_2m` | °C | `TemperatureCelsius` | `decimal(5,2)` | 2-meter ambient air temperature |
| `apparent_temperature` | °C | `ApparentTemperatureCelsius` | `decimal(5,2)` | Heat index / wind chill ("feels-like") |
| `relative_humidity_2m` | % | `RelativeHumidityPercent` | `decimal(5,2)` | Critical for fungal disease risk models |
| `dew_point_2m` | °C | `DewPointCelsius` | `decimal(5,2)` | Condensation & leaf wetness indicator |
| `precipitation` | mm | `PrecipitationMm` | `decimal(6,2)` | Total hourly rainfall liquid equivalent |
| `rain` | mm | `RainMm` | `decimal(6,2)` | Liquid rain component |
| `wind_speed_10m` | km/h | `WindSpeedKmh` | `decimal(5,2)` | 10-meter surface wind speed |
| `wind_gusts_10m` | km/h | `WindGustsKmh` | `decimal(5,2)` | Maximum short-duration wind gusts |
| `wind_direction_10m` | ° | `WindDirectionDegrees` | `int` | Compass direction (0° = North) |
| `surface_pressure` | hPa | `SurfacePressureHpa` | `decimal(8,2)` | Barometric pressure trend tracking |
| `shortwave_radiation` | W/m² | `ShortwaveRadiationWm2` | `decimal(7,2)` | Global horizontal solar irradiance |
| `direct_radiation` | W/m² | `DirectRadiationWm2` | `decimal(7,2)` | Direct beam solar irradiance |
| `diffuse_radiation` | W/m² | `DiffuseRadiationWm2` | `decimal(7,2)` | Scattered sky radiation |
| `et0_fao_evapotranspiration` | mm | `EvapotranspirationMm` | `decimal(5,2)` | FAO Penman-Monteith reference crop ET |
| `soil_temperature_0_to_7cm` | °C | `SoilTemperature0To7cm` | `decimal(5,2)` | Topsoil temperature (seed germination) |
| `soil_moisture_0_to_7cm` | m³/m³ | `SoilMoisture0To7cm` | `decimal(5,4)` | Surface volumetric soil moisture |
| `soil_moisture_7_to_28cm` | m³/m³ | `SoilMoisture7To28cm` | `decimal(5,4)` | Shallow root zone moisture |
| `soil_moisture_28_to_100cm`| m³/m³ | `SoilMoisture28To100cm` | `decimal(5,4)` | Deep root zone moisture |
| `soil_moisture_100_to_255cm`| m³/m³| `SoilMoisture100To255cm`| `decimal(5,4)` | Subsoil water table reservoir moisture |
| `weather_code` | Code | `WeatherCode` | `int` | WMO standard weather interpretation |

---

## 3. WMO Weather Code Mappings (`WeatherCodeMapper`)

The World Meteorological Organization (WMO) assigns numeric codes representing current and forecast weather conditions. The `WeatherCodeMapper` in `VarnAI.WeatherSense.Application` normalizes these into strongly-typed `WeatherCondition` enums:

| WMO Code | Mapped Condition (`WeatherCondition`) | Human-Readable Description |
| :--- | :--- | :--- |
| `0` | `ClearSky` | Clear sky |
| `1` | `MainlyClear` | Mainly clear sky |
| `2` | `PartlyCloudy` | Partly cloudy |
| `3` | `Overcast` | Overcast clouds |
| `45` | `Fog` | Fog |
| `48` | `DepositingRimeFog` | Depositing rime fog |
| `51` | `LightDrizzle` | Light drizzle |
| `53` | `ModerateDrizzle` | Moderate drizzle |
| `55` | `DenseDrizzle` | Dense intensity drizzle |
| `56` | `LightFreezingDrizzle` | Freezing drizzle: Light |
| `57` | `DenseFreezingDrizzle` | Freezing drizzle: Dense |
| `61` | `SlightRain` | Slight rain |
| `63` | `ModerateRain` | Moderate rain |
| `65` | `HeavyRain` | Heavy rain |
| `66` | `LightFreezingRain` | Freezing rain: Light |
| `67` | `HeavyFreezingRain` | Freezing rain: Heavy |
| `71` | `SlightSnowFall` | Slight snow fall |
| `73` | `ModerateSnowFall` | Moderate snow fall |
| `75` | `HeavySnowFall` | Heavy snow fall |
| `77` | `SnowGrains` | Snow grains |
| `80` | `SlightRainShowers` | Slight rain showers |
| `81` | `ModerateRainShowers` | Moderate rain showers |
| `82` | `ViolentRainShowers` | Violent rain showers |
| `85` | `SlightSnowShowers` | Slight snow showers |
| `86` | `HeavySnowShowers` | Heavy snow showers |
| `95` | `Thunderstorm` | Thunderstorm: Slight or moderate |
| `96` | `ThunderstormWithSlightHail`| Thunderstorm with slight hail |
| `99` | `ThunderstormWithHeavyHail` | Thunderstorm with heavy hail |
| *Other*| `Unknown` | Unclassified code fallback |

---

## 4. Resilience & Error Handling

To safeguard against external API degradation, the Open-Meteo HTTP client is configured with Microsoft Resilience defaults:

```csharp
builder.Services.AddHttpClient<IWeatherProvider, OpenMeteoWeatherProvider>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["OpenMeteo:BaseUrl"] ?? "https://api.open-meteo.com");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddStandardResilienceHandler();
```

### Protection Mechanisms:
1. **Exponential Backoff Retries**: Automatically retries transient 5xx server errors and HTTP 429 rate limit responses (up to 3 retries with random jitter).
2. **Circuit Breaking**: Temporarily halts outgoing requests if upstream error rates exceed 50%, allowing Open-Meteo to recover without hammering their API.
3. **Execution Isolation**: In the collection loop, any provider exception for a specific coordinate is caught, logged in `WeatherLocationExecutionDetails`, and stored as `Failed`, allowing all subsequent locations to process normally.

---

## 5. Extending to On-Farm IoT Weather Stations

VarnAI Farm Fresh operates physical farms in Tamil Nadu (e.g. Pollachi, Emmampoondi). In the future, farms will deploy on-site IoT microclimate sensors transmitting data over LoRaWAN or 4G/MQTT.

### Integration Path:
1. Create `IotStationWeatherProvider : IWeatherProvider`.
2. Register in Dependency Injection based on `WeatherLocation.DataSource` (e.g. `"OpenMeteo"` vs. `"FarmSensor"`).
3. The identical schema in `WeatherObservations` accommodates IoT observations with higher fidelity (e.g., 5-minute sampling or multi-depth soil probes).
