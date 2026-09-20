# ==============================================================================
# VARNAI WEATHERSENSE PRODUCTION IIS SETUP SCRIPT (Run as Administrator)
# Target IIS Path : C:\inetpub\wwwroot\VarnAIWeatherSense
# Target Port     : 8082 (http://192.168.1.101:8082 or http://localhost:8082)
# ==============================================================================

param(
    [int]$Port = 8082
)

$ErrorActionPreference = "Stop"

$siteName = "VarnAIWeatherSense"
$appPoolName = "VarnAIWeatherSense_Pool"
$targetPath = "C:\inetpub\wwwroot\VarnAIWeatherSense"
$sourceDir = "C:\Users\rmkar\.gemini\antigravity\scratch\Apps\VarnAI.WeatherSense"
$publishDir = "$sourceDir\PublishOutput"

Write-Host "======================================================" -ForegroundColor Cyan
Write-Host "   VARNAI WEATHERSENSE PRODUCTION IIS DEPLOYMENT      " -ForegroundColor Cyan
Write-Host "======================================================" -ForegroundColor Cyan
Write-Host "Target Port     : $Port" -ForegroundColor Yellow
Write-Host "Target Directory: $targetPath" -ForegroundColor Yellow
Write-Host "======================================================" -ForegroundColor Cyan

# 1. Administrator Check
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Error "Please run this script in an Administrator PowerShell window."
    exit 1
}

# 2. Import IIS Module
Import-Module WebAdministration -ErrorAction Stop

# 3. Publish WeatherSense API
Write-Host "`n[1/5] Building & Publishing VarnAI.WeatherSense.API (Release)..." -ForegroundColor Yellow
if (Test-Path $publishDir) { Remove-Item -Recurse -Force $publishDir -ErrorAction SilentlyContinue }
dotnet publish "$sourceDir\src\VarnAI.WeatherSense.API\VarnAI.WeatherSense.API.csproj" `
    -c Release `
    -o $publishDir `
    --self-contained false

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed!"
    exit $LASTEXITCODE
}

# 4. Stop existing AppPool / Release locks
Write-Host "`n[2/5] Stopping AppPool if existing..." -ForegroundColor Yellow
if (Test-Path "IIS:\AppPools\$appPoolName") {
    try {
        Stop-WebAppPool -Name $appPoolName -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
    } catch {}
}

# 5. Create Target Directory & Copy Files
Write-Host "`n[3/5] Deploying files to $targetPath..." -ForegroundColor Yellow
if (-not (Test-Path $targetPath)) {
    New-Item -ItemType Directory -Force -Path $targetPath | Out-Null
}

& robocopy.exe "$publishDir" "$targetPath" /E /NP /R:3 /W:2 | Out-Null

# 6. Configure AppPool & Website
Write-Host "`n[4/5] Configuring IIS AppPool & Website on Port $Port..." -ForegroundColor Yellow
if (-not (Test-Path "IIS:\AppPools\$appPoolName")) {
    New-WebAppPool -Name $appPoolName
    Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "managedRuntimeVersion" -Value ""
    Write-Host "Created App Pool '$appPoolName' (No Managed Code)." -ForegroundColor Green
}

if (-not (Test-Path "IIS:\Sites\$siteName")) {
    New-WebSite -Name $siteName -Port $Port -PhysicalPath $targetPath -ApplicationPool $appPoolName
    Write-Host "Created IIS Site '$siteName' on port $Port." -ForegroundColor Green
} else {
    Set-ItemProperty "IIS:\Sites\$siteName" -Name "physicalPath" -Value $targetPath
    Write-Host "Updated physicalPath for site '$siteName'." -ForegroundColor Green
}

# Grant AppPool permissions
try {
    & icacls "$targetPath" /grant "IIS AppPool\$appPoolName":(OI)(CI)M /T /Q | Out-Null
} catch {}

# 7. Start AppPool & Site
Write-Host "`n[5/5] Starting IIS Website..." -ForegroundColor Yellow
Start-WebAppPool -Name $appPoolName -ErrorAction SilentlyContinue
Start-WebSite -Name $siteName -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# Verify Health
try {
    $health = Invoke-RestMethod -Uri "http://localhost:$Port/health" -Method Get -TimeoutSec 5
    Write-Host "`n[SUCCESS] WeatherSense Production API is healthy: $health" -ForegroundColor Green
    Write-Host "Local URL  : http://localhost:$Port" -ForegroundColor Cyan
    Write-Host "Network URL: http://192.168.1.101:$Port" -ForegroundColor Cyan
    Write-Host "Swagger UI : http://192.168.1.101:$Port/swagger" -ForegroundColor Cyan
} catch {
    Write-Warning "Site deployed, but health check probe failed: $_"
}
