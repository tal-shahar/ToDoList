# Integration Test Script
Write-Host "=== API Integration Tests ===" -ForegroundColor Cyan

# Use Docker container port (8080)
$baseUrl = "http://localhost:8080/api"

Write-Host "Testing API at: $baseUrl" -ForegroundColor Yellow

try {
    # Test 1: Create User
    Write-Host "`n1. POST /api/users - Create User" -ForegroundColor Yellow
    $userBody = @{ name = "Integration Test User"; email = "test-integration@example.com" } | ConvertTo-Json
    $user = Invoke-RestMethod -Uri "$baseUrl/users" -Method POST -Body $userBody -ContentType "application/json" -ErrorAction Stop
    Write-Host "   Success: User created with ID $($user.id)" -ForegroundColor Green
    $userId = $user.id

    # Test 2: Get All Users
    Write-Host "`n2. GET /api/users - Get All Users" -ForegroundColor Yellow
    $users = Invoke-RestMethod -Uri "$baseUrl/users" -Method GET -ErrorAction Stop
    Write-Host "   Success: Retrieved $($users.Count) users" -ForegroundColor Green

    # Test 3: Get User by ID
    Write-Host "`n3. GET /api/users/$userId - Get User by ID" -ForegroundColor Yellow
    $userById = Invoke-RestMethod -Uri "$baseUrl/users/$userId" -Method GET -ErrorAction Stop
    Write-Host "   Success: Retrieved user $($userById.name)" -ForegroundColor Green

    # Test 4: Update User
    Write-Host "`n4. PUT /api/users/$userId - Update User" -ForegroundColor Yellow
    $updateBody = @{ name = "Updated Integration Test User"; email = "updated-test@example.com" } | ConvertTo-Json
    Invoke-RestMethod -Uri "$baseUrl/users/$userId" -Method PUT -Body $updateBody -ContentType "application/json" -ErrorAction Stop
    Write-Host "   Success: User updated" -ForegroundColor Green

    # Test 5: Create Item
    Write-Host "`n5. POST /api/items - Create Item" -ForegroundColor Yellow
    $itemBody = @{ title = "Test Todo Item"; description = "Test description"; userId = $userId } | ConvertTo-Json
    $item = Invoke-RestMethod -Uri "$baseUrl/items" -Method POST -Body $itemBody -ContentType "application/json" -ErrorAction Stop
    Write-Host "   Success: Item created with ID $($item.id)" -ForegroundColor Green
    $itemId = $item.id

    # Test 6: Get All Items
    Write-Host "`n6. GET /api/items - Get All Items" -ForegroundColor Yellow
    $items = Invoke-RestMethod -Uri "$baseUrl/items" -Method GET -ErrorAction Stop
    Write-Host "   Success: Retrieved $($items.Count) items" -ForegroundColor Green

    # Test 7: Get Item by ID
    Write-Host "`n7. GET /api/items/$itemId - Get Item by ID" -ForegroundColor Yellow
    $itemById = Invoke-RestMethod -Uri "$baseUrl/items/$itemId" -Method GET -ErrorAction Stop
    Write-Host "   Success: Retrieved item $($itemById.title)" -ForegroundColor Green

    # Test 8: Get Items by User ID
    Write-Host "`n8. GET /api/items/user/$userId - Get Items by User" -ForegroundColor Yellow
    $userItems = Invoke-RestMethod -Uri "$baseUrl/items/user/$userId" -Method GET -ErrorAction Stop
    Write-Host "   Success: Retrieved $($userItems.Count) items for user" -ForegroundColor Green

    # Test 9: Update Item
    Write-Host "`n9. PUT /api/items/$itemId - Update Item" -ForegroundColor Yellow
    $updateItemBody = @{ title = "Updated Todo Item"; description = "Updated description"; userId = $userId; isCompleted = $true } | ConvertTo-Json
    Invoke-RestMethod -Uri "$baseUrl/items/$itemId" -Method PUT -Body $updateItemBody -ContentType "application/json" -ErrorAction Stop
    Write-Host "   Success: Item updated" -ForegroundColor Green

    # Test 10: Delete Item
    Write-Host "`n10. DELETE /api/items/$itemId - Delete Item" -ForegroundColor Yellow
    Invoke-RestMethod -Uri "$baseUrl/items/$itemId" -Method DELETE -ErrorAction Stop
    Write-Host "   Success: Item deleted" -ForegroundColor Green

    # Test 11: Delete User
    Write-Host "`n11. DELETE /api/users/$userId - Delete User" -ForegroundColor Yellow
    Invoke-RestMethod -Uri "$baseUrl/users/$userId" -Method DELETE -ErrorAction Stop
    Write-Host "   Success: User deleted" -ForegroundColor Green

    Write-Host "`n=== All Integration Tests Passed! ===" -ForegroundColor Green
    
} catch {
    Write-Host "`n=== Integration Test Failed ===" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Stack: $($_.Exception.StackTrace)" -ForegroundColor Red
    exit 1
}
