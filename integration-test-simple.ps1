# Simple Integration Test Script
Write-Host "=== API Integration Tests ===" -ForegroundColor Cyan

$baseUrl = "http://localhost:8080/api"

# Test basic endpoint accessibility
Write-Host "`nTesting basic endpoints..." -ForegroundColor Yellow

try {
    # Test 1: Health Check
    Write-Host "`n1. GET /health - Health Check" -ForegroundColor Yellow
    $health = Invoke-RestMethod -Uri "http://localhost:8080/health" -Method GET -ErrorAction Stop
    Write-Host "   Success: API is healthy" -ForegroundColor Green

    # Test 2: Get All Users (should work even if empty)
    Write-Host "`n2. GET /api/users - Get All Users" -ForegroundColor Yellow
    $users = Invoke-RestMethod -Uri "$baseUrl/users" -Method GET -ErrorAction Stop
    Write-Host "   Success: Retrieved $($users.Count) users" -ForegroundColor Green

    Write-Host "`n=== Basic Integration Tests Passed ===" -ForegroundColor Green
    
} catch {
    Write-Host "`n=== Integration Test Failed ===" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

