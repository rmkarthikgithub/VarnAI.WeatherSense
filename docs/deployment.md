# VarnAI WeatherSense — Deployment Guide

## 1. Deployment Environments & Topologies

**VarnAI WeatherSense** supports three deployment topologies:
1. **Local Windows / IIS Hosting** (On-premises server / edge node)
2. **Containerized Deployment** (Docker & Docker Compose)
3. **Enterprise Cloud Deployment** (Azure App Service + Azure SQL Database)

---

## 2. Configuration & Environment Variables

| Variable | JSON Path | Default / Example | Purpose |
| :--- | :--- | :--- | :--- |
| `ASPNETCORE_ENVIRONMENT` | N/A | `Development` / `Production` | Controls logging verbosity, Swagger UI availability, and config overlays |
| `ConnectionStrings__DefaultConnection` | `ConnectionStrings:DefaultConnection` | `Server=.\\SQLEXPRESS;Database=VarnAI_WeatherSense_dev;...` | SQL Server connection string |
| `WeatherSenseSecurity__ApiKey` | `WeatherSenseSecurity:ApiKey` | `WeatherSenseDevSecretKey2026!#Secure` | Machine-to-machine authentication key |
| `OpenMeteo__BaseUrl` | `OpenMeteo:BaseUrl` | `https://api.open-meteo.com` | Base URL for the meteorological API |
| `Logging__LogLevel__Default` | `Logging:LogLevel:Default` | `Information` | Minimum logging level |

---

## 3. Containerized Deployment (Docker & Docker Compose)

### 3.1 Dockerfile Architecture
The solution includes a multi-stage production Dockerfile (`docker/Dockerfile`):
- **Build Stage**: Compiles and publishes binaries using the official `mcr.microsoft.com/dotnet/sdk:10.0` image.
- **Runtime Stage**: Ultra-lean, non-root Linux container based on `mcr.microsoft.com/dotnet/aspnet:10.0`.
- **Health Probes**: Embeds a native `HEALTHCHECK` using `curl` against `/health/live`.

### 3.2 Running via Docker Compose
To launch WeatherSense along with a local SQL Server 2022 instance:

```bash
# 1. Navigate to the project root
cd C:\Users\rmkar\.gemini\antigravity\scratch\Apps\VarnAI.WeatherSense

# 2. Build and launch containers
docker compose up -d --build

# 3. Verify container health
docker compose ps
```

The API will be accessible at:
- **Base API**: `http://localhost:8085`
- **Swagger UI**: `http://localhost:8085/swagger`
- **Health Check**: `http://localhost:8085/health`

---

## 4. Windows Server / IIS Deployment

### 4.1 Prerequisites
1. **.NET 10 Hosting Bundle**: Install the official .NET 10 ASP.NET Core Hosting Bundle on the target Windows Server.
2. **IIS URL Rewrite Module 2.1**.
3. **Application Pool**: Create an AppPool named `VarnAIWeatherSenseAppPool` with:
   - **.NET CLR Version**: `No Managed Code`
   - **Managed Pipeline Mode**: `Integrated`
   - **Identity**: `ApplicationPoolIdentity` (or dedicated service account with database permissions).

### 4.2 Build & Publish Command
```powershell
# Publish the API project in Release mode
dotnet publish src/VarnAI.WeatherSense.API/VarnAI.WeatherSense.API.csproj `
  -c Release `
  -o C:\publish\WeatherSense `
  --self-contained false
```

### 4.3 Copy to IIS Web Root
```powershell
# Stop IIS website (or app pool)
Stop-WebAppPool -Name "VarnAIWeatherSenseAppPool"

# Mirror published files to IIS folder
robocopy "C:\publish\WeatherSense" "C:\inetpub\wwwroot\VarnAIWeatherSense" /MIR /R:2 /W:1 /NP

# Restart Application Pool
Start-WebAppPool -Name "VarnAIWeatherSenseAppPool"
```

---

## 5. Enterprise Cloud Deployment (Azure)

### 5.1 Architecture
- **Web App**: Azure App Service Linux (.NET 10 LTS container or code runtime).
- **Database**: Azure SQL Database (General Purpose Serverless or DTU S1).
- **Secrets Management**: Azure Key Vault or App Service Application Settings.
- **Orchestration**: Self-hosted n8n container in Azure Container Apps, or Azure Logic Apps hitting `POST /api/v1/weather/collect`.

### 5.2 Azure App Service Configuration
Set the following Application Settings in Azure Portal:
```bash
ConnectionStrings__DefaultConnection="Server=tcp:varnai-sql.database.windows.net,1433;Initial Catalog=VarnAI_WeatherSense;Persist Security Info=False;User ID=varnai_admin;Password=YourSecurePassword!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
WeatherSenseSecurity__ApiKey="YourStrongAzureSecretKey_987654321!"
ASPNETCORE_ENVIRONMENT="Production"
```

### 5.3 Health Probes in Azure
In the Azure Portal, configure the **Health Check** blade:
- Path: `/health/ready`
- Interval: 1 minute
- Action: Reroute traffic or restart unhealthy instances if `/health/ready` returns HTTP 503.

---

## 6. Database Migration in Production

Always execute migrations before routing traffic to a new version:

```powershell
# Apply EF Core migrations directly to the target database
dotnet ef database update `
  --project src/VarnAI.WeatherSense.Infrastructure `
  --startup-project src/VarnAI.WeatherSense.API `
  --connection "Server=your-prod-sql;Database=VarnAI_WeatherSense;User Id=...;Password=...;"
```
*(Alternatively, generate idempotent SQL migration scripts using `dotnet ef migrations script --idempotent -o migrate.sql` and review with your DBA before execution).*
