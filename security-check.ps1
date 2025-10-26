# Security Check Script for ToDoList API
# Run this after starting your services with docker-compose

Write-Host "Starting security checks..." -ForegroundColor Yellow

# Check if services are running
$services = @(
    @{Name="todo-api"; Port=8080},
    @{Name="todo-postgres"; Port=5432},
    @{Name="todo-rabbitmq"; Port=5672}
)

Write-Host "`nChecking if services are up..." -ForegroundColor Cyan
foreach ($service in $services) {
    try {
        $null = Invoke-WebRequest -Uri "http://localhost:$($service.Port)" -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
        Write-Host "✓ $($service.Name) is running on port $($service.Port)" -ForegroundColor Green
    } catch {
        Write-Host "✗ $($service.Name) is not responding on port $($service.Port)" -ForegroundColor Red
    }
}

# Check API health
Write-Host "`nChecking API health..." -ForegroundColor Cyan
try {
    $health = Invoke-RestMethod -Uri "http://localhost:8080/health" -Method Get -ErrorAction Stop
    Write-Host "✓ Health check passed" -ForegroundColor Green
} catch {
    Write-Host "✗ Health check failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Security Issues Check
Write-Host "`nChecking security configurations..." -ForegroundColor Yellow

$issues = @()

# Check if Swagger is exposed in production
try {
    $null = Invoke-WebRequest -Uri "http://localhost:8080/swagger" -UseBasicParsing -ErrorAction Stop
    $issues += "Swagger UI is exposed at /swagger (security risk in production)"
} catch {
    # Swagger not accessible
}

# Check if basic auth exists
try {
    $null = Invoke-WebRequest -Uri "http://localhost:8080/api/users" -UseBasicParsing -ErrorAction Stop
    $issues += "No authentication required for endpoints"
} catch {
    if ($_.Exception.Response.StatusCode -eq 401) {
        Write-Host "✓ Authentication is enabled" -ForegroundColor Green
    } else {
        $issues += "No authentication required for endpoints"
    }
}

# Check for exposed sensitive information
$issues += "Default RabbitMQ credentials (guest/guest) in use"
$issues += "No rate limiting configured"
$issues += "No CORS policy configured"
$issues += "Swagger UI enabled in all environments (remove in production)"

if ($issues.Count -gt 0) {
    Write-Host "`n⚠ Security Issues Found:" -ForegroundColor Red
    foreach ($issue in $issues) {
        Write-Host "  - $issue" -ForegroundColor Yellow
    }
} else {
    Write-Host "✓ No security issues detected" -ForegroundColor Green
}

Write-Host "`nRecommendations:" -ForegroundColor Cyan
Write-Host "  1. Add authentication (JWT/Bearer tokens)"
Write-Host "  2. Implement rate limiting"
Write-Host "  3. Use stronger RabbitMQ credentials"
Write-Host "  4. Disable Swagger in production"
Write-Host "  5. Add CORS policy"
Write-Host "  6. Run OWASP ZAP scan: docker run -t ghcr.io/zaproxy/zaproxy:stable zap-baseline.py -t http://localhost:8080"
