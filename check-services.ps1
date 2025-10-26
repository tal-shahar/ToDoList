Write-Host "=== Service Status Check ===" -ForegroundColor Cyan
Write-Host ""

# Check RabbitMQ
Write-Host "1. RabbitMQ (port 5672):" -ForegroundColor Yellow
if (Test-NetConnection -ComputerName localhost -Port 5672 -InformationLevel Quiet -WarningAction SilentlyContinue) {
    Write-Host "   Status: Running" -ForegroundColor Green
} else {
    Write-Host "   Status: Not Running" -ForegroundColor Red
}

# Check PostgreSQL
Write-Host "2. PostgreSQL (port 5432):" -ForegroundColor Yellow
if (Test-NetConnection -ComputerName localhost -Port 5432 -InformationLevel Quiet -WarningAction SilentlyContinue) {
    Write-Host "   Status: Running" -ForegroundColor Green
} else {
    Write-Host "   Status: Not Running" -ForegroundColor Red
}

# Check API
Write-Host "3. ToDoList API (port 5142):" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri http://localhost:5142/health -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
    Write-Host "   Status: Running - $($response.Content)" -ForegroundColor Green
} catch {
    Write-Host "   Status: Not Running" -ForegroundColor Red
}

# Check Swagger
Write-Host "4. Swagger UI:" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri http://localhost:5142/swagger/index.html -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
    Write-Host "   Status: Accessible" -ForegroundColor Green
} catch {
    Write-Host "   Status: Not Accessible" -ForegroundColor Red
}

Write-Host ""

