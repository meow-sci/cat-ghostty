# Test script for validating terminal escape sequences
# This script tests various escape sequences to ensure they work correctly

Write-Host "Testing caTTY Terminal Escape Sequences" -ForegroundColor Green
Write-Host "=======================================" -ForegroundColor Green
Write-Host ""

# Test 1: Basic cursor movement
Write-Host "Test 1: Cursor Movement" -ForegroundColor Yellow
Write-Host "Moving cursor up 2 lines, then down 1 line..."
Write-Host "`e[2A`e[1B[Cursor moved]"
Write-Host ""

# Test 2: Cursor positioning
Write-Host "Test 2: Cursor Positioning" -ForegroundColor Yellow
Write-Host "Setting cursor to position (5,10)..."
Write-Host "`e[5;10H[At position 5,10]"
Write-Host ""

# Test 3: Screen clearing
Write-Host "Test 3: Screen Clearing" -ForegroundColor Yellow
Write-Host "Clearing from cursor to end of screen in 3 seconds..."
Start-Sleep -Seconds 1
Write-Host "3..."
Start-Sleep -Seconds 1
Write-Host "2..."
Start-Sleep -Seconds 1
Write-Host "1..."
Write-Host "`e[0J"
Write-Host "Screen cleared from cursor to end"
Write-Host ""

# Test 4: Line clearing
Write-Host "Test 4: Line Clearing" -ForegroundColor Yellow
Write-Host "This line will be partially cleared`e[0K <- cleared to end"
Write-Host ""

# Test 5: Tab operations
Write-Host "Test 5: Tab Operations" -ForegroundColor Yellow
Write-Host "Tab`tTab`tTab`tEnd"
Write-Host "Forward tab:`e[I[After forward tab]"
Write-Host ""

# Test 6: Device queries (these should generate responses)
Write-Host "Test 6: Device Queries" -ForegroundColor Yellow
Write-Host "Sending device attribute query (should get response)..."
Write-Host "`e[c"
Write-Host "Sending cursor position report query..."
Write-Host "`e[6n"
Write-Host ""

# Test 7: Save/Restore cursor
Write-Host "Test 7: Save/Restore Cursor" -ForegroundColor Yellow
Write-Host "Saving cursor position`e7"
Write-Host "Moving cursor and writing text`e[10;20HMoved text"
Write-Host "Restoring cursor position`e8[Back to saved position]"
Write-Host ""

# Test 8: UTF-8 characters
Write-Host "Test 8: UTF-8 Characters" -ForegroundColor Yellow
Write-Host "Testing UTF-8: Hello 世界 🌍 piñata"
Write-Host ""

# Test 9: Control characters
Write-Host "Test 9: Control Characters" -ForegroundColor Yellow
Write-Host "Bell (should make sound): `a"
Write-Host "Backspace test: ABC`b`bXY (should show AXY)"
Write-Host ""

Write-Host "Escape sequence testing complete!" -ForegroundColor Green
Write-Host "If you can see this message and the cursor movements worked," -ForegroundColor Green
Write-Host "then basic escape sequence parsing is functional." -ForegroundColor Green