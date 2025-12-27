#!/usr/bin/env pwsh

Write-Host "caTTY Scrolling Functionality Validation" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host

# Run automated tests
Write-Host "Step 1: Running Automated Tests" -ForegroundColor Magenta
Write-Host "===============================" -ForegroundColor Magenta
Write-Host

Write-Host "Running scrolling validation tests..." -ForegroundColor Yellow
dotnet test --filter "ScrollingValidationTests" --verbosity minimal --nologo
if ($LASTEXITCODE -eq 0) {
    Write-Host "Success: Integration tests passed" -ForegroundColor Green
} else {
    Write-Host "Error: Some integration tests failed" -ForegroundColor Red
}

Write-Host
Write-Host "Running property-based tests..." -ForegroundColor Yellow
dotnet test --filter "ScrollbackBufferProperties" --verbosity minimal --nologo
if ($LASTEXITCODE -eq 0) {
    Write-Host "Success: Scrollback property tests passed" -ForegroundColor Green
} else {
    Write-Host "Error: Some scrollback property tests failed" -ForegroundColor Red
}

dotnet test --filter "ScreenScrollingProperties" --verbosity minimal --nologo
if ($LASTEXITCODE -eq 0) {
    Write-Host "Success: Screen scrolling property tests passed" -ForegroundColor Green
} else {
    Write-Host "Error: Some screen scrolling property tests failed" -ForegroundColor Red
}

Write-Host
Write-Host "Step 2: Build Validation" -ForegroundColor Magenta
Write-Host "========================" -ForegroundColor Magenta
Write-Host

Write-Host "Building TestApp..." -ForegroundColor Yellow
dotnet build caTTY.TestApp --verbosity minimal --nologo
if ($LASTEXITCODE -eq 0) {
    Write-Host "Success: TestApp built successfully" -ForegroundColor Green
} else {
    Write-Host "Error: TestApp build failed" -ForegroundColor Red
}

Write-Host "Building GameMod..." -ForegroundColor Yellow
dotnet build caTTY.GameMod --verbosity minimal --nologo
if ($LASTEXITCODE -eq 0) {
    Write-Host "Success: GameMod built successfully" -ForegroundColor Green
} else {
    Write-Host "Error: GameMod build failed" -ForegroundColor Red
}

Write-Host
Write-Host "Step 3: Manual Validation Required" -ForegroundColor Magenta
Write-Host "==================================" -ForegroundColor Magenta
Write-Host

Write-Host "TestApp Manual Testing:" -ForegroundColor Cyan
Write-Host "1. Run: cd caTTY.TestApp; dotnet run" -ForegroundColor White
Write-Host "2. Test long output: for /L %i in (1,1,50) do echo Line %i" -ForegroundColor White
Write-Host "3. Test scrollback navigation with mouse wheel" -ForegroundColor White
Write-Host "4. Test window resize during operation" -ForegroundColor White
Write-Host

Write-Host "GameMod Manual Testing:" -ForegroundColor Cyan
Write-Host "1. Start KSA game and load caTTY GameMod" -ForegroundColor White
Write-Host "2. Press F12 to toggle terminal" -ForegroundColor White
Write-Host "3. Test same scrolling scenarios as TestApp" -ForegroundColor White
Write-Host "4. Verify no interference with game" -ForegroundColor White
Write-Host

Write-Host "Validation Checklist:" -ForegroundColor Yellow
Write-Host "[ ] Long command output scrolls correctly" -ForegroundColor White
Write-Host "[ ] Content preserved in scrollback buffer" -ForegroundColor White
Write-Host "[ ] Manual scrollback navigation works" -ForegroundColor White
Write-Host "[ ] Auto-scroll enables/disables correctly" -ForegroundColor White
Write-Host "[ ] Viewport does not yank during history review" -ForegroundColor White
Write-Host "[ ] Content preserved during resize" -ForegroundColor White
Write-Host "[ ] TestApp scrolling works correctly" -ForegroundColor White
Write-Host "[ ] GameMod scrolling works correctly" -ForegroundColor White
Write-Host

Write-Host "For detailed instructions, see: SCROLLING_VALIDATION_GUIDE.md" -ForegroundColor Gray
Write-Host "Validation complete. Please perform manual testing as described above." -ForegroundColor Cyan