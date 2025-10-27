# Comprehensive Security Scan for ToDoList Application
# This script runs multiple security scanners

param(
    [switch]$Quick = $false,
    [string]$ApiUrl = "http://localhost:8080"
)

Write-Host "`n=== Security Scan for ToDoList Application ===`n" -ForegroundColor Cyan

# Function to check if a command exists
function Test-Command {
    param([string]$Command)
    try {
        Get-Command $Command -ErrorAction Stop | Out-Null
        return $true
    } catch {
        return $false
    }
}

# 1. Trivy Container Scan
Write-Host "`n[1/5] Running Trivy container vulnerability scan..." -ForegroundColor Yellow
if (Test-Command "trivy") {
    Write-Host "Scanning Docker images..." -ForegroundColor Cyan
    docker images --format "{{.Repository}}:{{.Tag}}" | Select-String "todo" | ForEach-Object {
        Write-Host "Scanning $_..." -ForegroundColor Gray
        trivy image $_ --severity HIGH,CRITICAL
    }
} else {
    Write-Host "Trivy not installed. Install: https://github.com/aquasecurity/trivy" -ForegroundColor Yellow
}

# 2. Docker Compose Security
Write-Host "`n[2/5] Scanning Docker Compose configuration..." -ForegroundColor Yellow
if (Test-Command "trivy") {
    trivy config docker-compose.yml
} else {
    Write-Host "Installing Docker secrets management..." -ForegroundColor Yellow
}

# 3. Dependency Vulnerabilities
Write-Host "`n[3/5] Checking .NET dependencies..." -ForegroundColor Yellow
Write-Host "Running dotnet list package --vulnerable" -ForegroundColor Cyan
dotnet list package --vulnerable --include-transitive

# 4. Code Secrets Scan
Write-Host "`n[4/5] Scanning for exposed secrets..." -ForegroundColor Yellow
if (Test-Command "gitleaks") {
    gitleaks detect --source . --verbose
} else {
    Write-Host "gitleaks not installed. Install: https://github.com/gitleaks/gitleaks" -ForegroundColor Yellow
    Write-Host "Manually checking appsettings.json for hardcoded credentials..." -ForegroundColor Cyan
    
    # Check for common patterns
    $sensitivePatterns = @(
        "password\s*=\s*[\""'].*[\""']",
        "pwd\s*=\s*[\""'].*[\""']",
        "secret\s*=\s*[\""'].*[\""']",
        "apikey\s*=\s*[\""'].*[\""']",
        "guest"
    )
    
    Get-ChildItem -Recurse -Include "*.json", "*.cs", "*.env" -Exclude "*bin*", "*obj*" | ForEach-Object {
        $content = Get-Content $_.FullName -Raw
        foreach ($pattern in $sensitivePatterns) {
            if ($content -match $pattern) {
                Write-Host "WARNING: Found potential secret in: $($_.FullName)" -ForegroundColor Yellow
            }
        }
    }
}

# 5. OWASP ZAP Scan (if API is running)
Write-Host "`n[5/5] Running OWASP ZAP baseline scan..." -ForegroundColor Yellow
if (Test-Command "docker") {
    try {
        Write-Host "Attempting to access API at $ApiUrl..." -ForegroundColor Cyan
        $response = Invoke-WebRequest -Uri $ApiUrl -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
        Write-Host "API is accessible" -ForegroundColor Green
        
        Write-Host "Starting OWASP ZAP scan (this may take 5-10 minutes)..." -ForegroundColor Cyan
        docker run -t --rm owasp/zap2docker-stable zap-baseline.py -t $ApiUrl -J zap-report.json
        Write-Host "ZAP scan completed" -ForegroundColor Green
    } catch {
        Write-Host "WARNING: API not accessible at $ApiUrl. Start services first: docker-compose up" -ForegroundColor Red
        if (-not $Quick) {
            Write-Host "Skipping OWASP ZAP scan" -ForegroundColor Gray
        }
    }
} else {
    Write-Host "Docker not installed. Skipping OWASP ZAP scan." -ForegroundColor Yellow
}

Write-Host "`n=== Scan Complete ===" -ForegroundColor Cyan
Write-Host "`nRecommended Actions:" -ForegroundColor Yellow
Write-Host "1. Review and fix any HIGH/CRITICAL vulnerabilities"
Write-Host "2. Remove hardcoded credentials from config files"
Write-Host "3. Use Docker secrets or environment variables for sensitive data"
Write-Host "4. Configure authentication and authorization"
Write-Host "5. Disable Swagger UI in production"
Write-Host "6. Enable rate limiting in production"
Write-Host "7. Add CORS policy"
