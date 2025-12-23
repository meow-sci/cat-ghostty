# caTTY Integration Validation Script
# This script helps validate both TestApp and GameMod functionality

Write-Host "caTTY Integration Validation" -ForegroundColor Green
Write-Host "===========================" -ForegroundColor Green
Write-Host ""

# Check build status
Write-Host "1. Building solution..." -ForegroundColor Yellow
$buildResult = dotnet build --verbosity quiet
if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ Build successful" -ForegroundColor Green
} else {
    Write-Host "   ❌ Build failed" -ForegroundColor Red
    exit 1
}

# Run tests
Write-Host "2. Running tests..." -ForegroundColor Yellow
$testResult = dotnet test --verbosity quiet --logger "console;verbosity=minimal"
if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ Tests passed" -ForegroundColor Green
} else {
    Write-Host "   ❌ Tests failed" -ForegroundColor Red
    exit 1
}

# Check GameMod DLL
Write-Host "3. Checking GameMod output..." -ForegroundColor Yellow
if (Test-Path "caTTY.GameMod\bin\Debug\net10.0\caTTY.dll") {
    Write-Host "   ✅ GameMod DLL built successfully" -ForegroundColor Green
} else {
    Write-Host "   ❌ GameMod DLL not found" -ForegroundColor Red
    exit 1
}

# Check mod.toml
if (Test-Path "caTTY.GameMod\mod.toml") {
    Write-Host "   ✅ mod.toml configuration found" -ForegroundColor Green
} else {
    Write-Host "   ❌ mod.toml not found" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Automated validation complete! ✅" -ForegroundColor Green
Write-Host ""
Write-Host "Manual Testing Instructions:" -ForegroundColor Cyan
Write-Host "===========================" -ForegroundColor Cyan
Write-Host ""
Write-Host "TestApp Testing:" -ForegroundColor Yellow
Write-Host "1. cd caTTY.TestApp"
Write-Host "2. dotnet run"
Write-Host "3. Test basic commands: ls, dir, echo 'Hello World'"
Write-Host "4. Test keyboard input: arrow keys, Ctrl+C"
Write-Host "5. Verify terminal window displays correctly"
Write-Host ""
Write-Host "GameMod Testing:" -ForegroundColor Yellow
Write-Host "1. Copy caTTY.GameMod\bin\Debug\net10.0\caTTY.dll to KSA mods folder"
Write-Host "2. Copy caTTY.GameMod\mod.toml to the same location"
Write-Host "3. Start KSA game"
Write-Host "4. Press F12 to toggle terminal"
Write-Host "5. Test same commands as TestApp"
Write-Host "6. Verify terminal works within game context"
Write-Host ""
Write-Host "Both applications should:" -ForegroundColor Magenta
Write-Host "- Use the same ImGui controller and rendering code"
Write-Host "- Display terminal content correctly"
Write-Host "- Handle keyboard input properly"
Write-Host "- Start and manage shell processes"
Write-Host "- Clean up resources on exit"