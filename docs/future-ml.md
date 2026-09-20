# VarnAI WeatherSense — Future Machine Learning & Agri-Analytics Guide

## 1. Environmental Data as an Enterprise AI Asset

Agricultural intelligence requires continuous, multi-dimensional time-series data. By collecting hourly weather, atmospheric radiation, soil dynamics across four depths, and forecast snapshot histories, **VarnAI WeatherSense** creates an enterprise-grade dataset for machine learning models.

```mermaid
graph TD
    WS_API[VarnAI WeatherSense API<br/>GET /api/v1/weather/locations/{id}/dataset] --> PY_PIPELINE[Python / Pandas / PyTorch Pipeline]
    
    subgraph Agri-ML Models
        MODEL_IRRIG[Smart Irrigation Model<br/>ET₀ & Soil Moisture Depletion]
        MODEL_DAIRY[Dairy Heat Stress Model<br/>THI & Milk Yield Prediction]
        MODEL_DISEASE[Fungal Disease Risk Model<br/>Leaf Wetness & Spore Growth]
        MODEL_YIELD[Yield & Harvest Timing Model<br/>GDD & Solar Radiation Integral]
    end

    PY_PIPELINE --> MODEL_IRRIG
    PY_PIPELINE --> MODEL_DAIRY
    PY_PIPELINE --> MODEL_DISEASE
    PY_PIPELINE --> MODEL_YIELD

    MODEL_IRRIG --> ACTUATOR[Automated Drip Valves]
    MODEL_DAIRY --> SHED_FANS[Shed Misters & Fans]
    MODEL_DISEASE --> SPRAY_ALERT[Targeted Bio-Pesticide Plan]
    MODEL_YIELD --> LOGISTICS[VarnaiFresh Dispatch Hub]
```

---

## 2. The `/dataset` Endpoint

The API provides a dedicated machine learning ingestion endpoint:
```http
GET /api/v1/weather/locations/{locationId}/dataset?fromUtc=2026-01-01T00:00:00Z&toUtc=2026-09-01T00:00:00Z
```

### Pre-Engineered Attributes:
- **Zero Nested Objects**: Completely flat JSON objects ready for direct conversion into a `pandas.DataFrame`.
- **Temporal Alignment**: Hourly intervals in UTC ISO 8601.
- **Atmospheric Features**: Temperature, apparent temperature, relative humidity, dew point, surface pressure, wind speed, wind gusts, wind direction.
- **Solar Energy Features**: Direct radiation, diffuse radiation, shortwave radiation (W/m²).
- **Subsurface Matrix**: Soil temperature (0-7cm) and volumetric soil moisture across 4 depth horizons (0-7cm, 7-28cm, 28-100cm, 100-255cm).
- **Evaporative Demand**: FAO reference evapotranspiration (ET₀).

---

## 3. Core Agricultural ML Use Cases

### 3.1 Smart Irrigation Scheduling (ET₀ + Soil Moisture Deficit)
- **Problem**: Over-irrigation wastes water and leaches soil nutrients; under-irrigation induces plant moisture stress, stunting crop development.
- **Feature Inputs**:
  - `EvapotranspirationMm` (ET₀)
  - `PrecipitationMm`
  - `SoilMoisture0To7cm` (Surface layer)
  - `SoilMoisture7To28cm` (Active root zone)
  - `Forecasts` (Incoming rain probability over next 24 hours)
- **Mathematical Principle**:
  $$\Delta \text{Soil Water Storage} = \text{Precipitation} + \text{Irrigation} - \text{ET}_c - \text{Percolation}$$
- **Model Output**: Exact volume of drip irrigation water (liters/plant) required for tomorrow morning, automatically withheld if rainfall probability > 70%.

---

### 3.2 Dairy Cattle Heat Stress & Milk Yield Forecasting
- **Problem**: Dairy cows (HF/Jersey crosses common in Tamil Nadu) experience severe thermal stress in warm, humid weather, resulting in an immediate 10% to 30% drop in daily milk yield.
- **Feature Inputs**:
  - `TemperatureCelsius` ($T$)
  - `RelativeHumidityPercent` ($RH$)
- **Temperature-Humidity Index (THI) Formula**:
  $$\text{THI} = 0.8 \times T + \frac{RH}{100} \times (T - 14.4) + 46.4$$
- **Stress Classification**:
  - $\text{THI} < 72$: Comfort zone (Normal milk production).
  - $72 \le \text{THI} < 79$: Mild stress (Reduced feed intake, 1-2 liter milk drop).
  - $79 \le \text{THI} < 89$: Moderate to severe stress (Respiration rate spikes, dangerous yield drop).
  - $\text{THI} \ge 89$: Severe stress (Emergency risk).
- **Model Action**: When 48-hour forecast THI exceeds 74, WeatherSense triggers alerts to activate shed ventilation fans, shade curtains, and evaporative misting systems 4 hours before peak heat.

---

### 3.3 Crop Disease & Fungal Outbreak Modeling
- **Problem**: Fungal pathogens such as Downy Mildew, Early Blight, and Anthracnose require specific temperature windows combined with extended relative humidity to germinate.
- **Feature Inputs**:
  - Number of consecutive hours with `RelativeHumidityPercent` > 85% and $18^\circ\text{C} \le \text{TemperatureCelsius} \le 28^\circ\text{C}$.
  - `DewPointCelsius` proximity to `TemperatureCelsius` (indicating leaf condensation).
- **Model Output**: Outbreak Probability Index (0.00 to 1.00). Alerts agronomists to apply preventative organic neem/bio-fungicide sprays before visual symptoms appear.

---

### 3.4 Growing Degree Days (GDD) & Harvest Window Logistics
- **Problem**: Fresh produce supply chains (VarnaiFresh) need accurate harvest dates 14 days in advance to schedule packhouse labor and refrigerated transport.
- **Feature Inputs**: Daily maximum and minimum temperatures.
- **Growing Degree Days Calculation**:
  $$\text{GDD} = \max\left(0, \frac{T_{\text{max}} + T_{\text{min}}}{2} - T_{\text{base}}\right)$$
  *(where $T_{\text{base}}$ is crop-specific, e.g. $10^\circ\text{C}$ for tomatoes and maize).*
- **Cumulative Thermal Units**: By projecting accumulated GDD using forecast snapshots, VarnAI accurately predicts peak harvest dates within $\pm 1$ day.

---

## 4. Python / Pandas / PyTorch Integration Example

The following Python script demonstrates how a data scientist at VarnAI can ingest WeatherSense data and compute thermal indices in 10 lines of code:

```python
import requests
import pandas as pd
import numpy as np

# 1. Query WeatherSense ML Dataset
API_URL = "http://localhost:5270/api/v1/weather/locations/1/dataset"
HEADERS = {"X-API-Key": "varnai-weathersense-dev-key-2026"}
PARAMS = {
    "fromUtc": "2026-08-01T00:00:00Z",
    "toUtc": "2026-09-19T00:00:00Z"
}

response = requests.get(API_URL, headers=HEADERS, params=PARAMS)
response.raise_for_status()

# 2. Load into Pandas DataFrame
df = pd.DataFrame(response.json())
df['timestampUtc'] = pd.to_datetime(df['timestampUtc'])
df.set_index('timestampUtc', inplace=True)

# 3. Compute Dairy Temperature-Humidity Index (THI)
t = df['temperatureCelsius']
rh = df['relativeHumidityPercent']
df['thi'] = (0.8 * t) + (rh / 100.0) * (t - 14.4) + 46.4

# 4. Filter High Heat Stress Windows
heat_stress_hours = df[df['thi'] >= 72.0]
print(f"Total Heat Stress Hours Detected: {len(heat_stress_hours)}")
print(heat_stress_hours[['temperatureCelsius', 'relativeHumidityPercent', 'thi']].head())
```
