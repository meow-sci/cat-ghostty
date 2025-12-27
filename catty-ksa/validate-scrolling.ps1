#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Validates scrolling functionality in caTTY terminal emulator applications.

.DESCRIPTION
    This script provides automated validation of scrolling functionality for both
    the TestApp and GameMod implementations of the caTTY terminal emulator.

.PARAMETER TestApp
    Run validation tests for the TestApp only.

.PARAMETER GameMod
    Run validation tests for the GameMod only (requires KSA to be running).

.PARAMETER All
    Run all validation tests (default).

.EXAMPLE
    .\validate-scrolling.ps1
    Runs all validation tests.

.EXAMPLE
    .\validate-scrolling.ps1 -TestApp
    Runs validation tests for TestApp only.
#>

param(
    [switch]$TestApp,
    [switch]$GameMod,
    [switch]$All = $true
)

# Set default behavior
if (-not $TestApp -and -not $GameMod) {
    $All = $true
}

Write-Host "caTTY Scrolling Functionality Validation" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host

# Function to run automated tests
function Run-AutomatedTests {
    Write-Host "Running automated scrolling validation tests..." -ForegroundColor Yellow
    
    try {
        # Run integration tests
        $result = dotnet test --filter "ScrollingValidationTests" --verbosity minimal --nologo
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ All automated tests passed" -ForegroundColor Green
            return $true
        } else {
            Write-Host "✗ Some automated tests failed" -ForegroundColor Red
            Write-Host $result
            return $false
        }
    } catch {
        Write-Host "✗ Error running automated tests: $_" -ForegroundColor Red
        return $false
    }
}

# Function to run property-based tests
function Run-PropertyTests {
    Write-Host "Running property-based scrolling tests..." -ForegroundColor Yellow
    
    try {
        # Run scrollback and screen scrolling property tests
        $result = dotnet test --filter "ScrollbackBufferProperties|ScreenScrollingProperties" --verbosity minimal --nologo
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ All property tests passed" -ForegroundColor Green
            return $true
        } else {
            Write-Host "✗ Some property tests failed" -ForegroundColor Red
            Write-Host $result
            return $false
        }
    } catch {
        Write-Host "✗ Error running property tests: $_" -ForegroundColor Red
        return $false
    }
}

# Function to validate TestApp
function Test-TestApp {
    Write-Host "Validating TestApp scrolling functionality..." -ForegroundColor Yellow
    Write-Host
    
    # Check if TestApp can be built
    Write-Host "Building TestApp..." -ForegroundColor Gray
    $buildResult = dotnet build caTTY.TestApp --verbosity minimal --nologo
    if ($LASTEXITCODE -ne 0) {
        Write-Host "✗ TestApp build failed" -ForegroundColor Red
        return $false
    }
    Write-Host "✓ TestApp built successfully" -ForegroundColor Green
    
    # Provide manual testing instructions
    Write-Host
    Write-Host "Manual TestApp Validation Required:" -ForegroundColor Cyan
    Write-Host "1. Run: cd caTTY.TestApp; dotnet run" -ForegroundColor White
    Write-Host "2. Test long command output scrolling:" -ForegroundColor White
    Write-Host "   - Windows: dir /s C:\Windows\System32" -ForegroundColor Gray
    Write-Host "   - Or: for /L %i in (1,1,50) do echo Line %i" -ForegroundColor Gray
    Write-Host "3. Test viewport navigation:" -ForegroundColor White
    Write-Host "   - Scroll up with mouse wheel" -ForegroundColor Gray
    Write-Host "   - Verify auto-scroll disables" -ForegroundColor Gray
    Write-Host "   - Scroll back to bottom" -ForegroundColor Gray
    Write-Host "   - Verify auto-scroll re-enables" -ForegroundColor Gray
    Write-Host "4. Test resize handling:" -ForegroundColor White
    Write-Host "   - Resize window during operation" -ForegroundColor Gray
    Write-Host "   - Verify content preservation" -ForegroundColor Gray
    Write-Host
    
    return $true
}

# Function to validate GameMod
function Test-GameMod {
    Write-Host "Validating GameMod scrolling functionality..." -ForegroundColor Yellow
    Write-Host
    
    # Check if GameMod can be built
    Write-Host "Building GameMod..." -ForegroundColor Gray
    $buildResult = dotnet build caTTY.GameMod --verbosity minimal --nologo
    if ($LASTEXITCODE -ne 0) {
        Write-Host "✗ GameMod build failed" -ForegroundColor Red
        return $false
    }
    Write-Host "✓ GameMod built successfully" -ForegroundColor Green
    
    # Provide manual testing instructions
    Write-Host
    Write-Host "Manual GameMod Validation Required:" -ForegroundColor Cyan
    Write-Host "1. Start KSA game" -ForegroundColor White
    Write-Host "2. Load the caTTY GameMod" -ForegroundColor White
    Write-Host "3. Press F12 to toggle terminal" -ForegroundColor White
    Write-Host "4. Test same scrolling scenarios as TestApp:" -ForegroundColor White
    Write-Host "   - Long command output" -ForegroundColor Gray
    Write-Host "   - Viewport navigation" -ForegroundColor Gray
    Write-Host "   - Resize handling" -ForegroundColor Gray
    Write-Host "5. Verify no interference with game input/rendering" -ForegroundColor White
    Write-Host "6. Test terminal toggle (F12) during scrolling" -ForegroundColor White
    Write-Host
    
    return $true
}

# Function to display validation checklist
function Show-ValidationChecklist {
    Write-Host "Scrolling Validation Checklist:" -ForegroundColor Cyan
    Write-Host "==============================" -ForegroundColor Cyan
    Write-Host
    Write-Host "Basic Functionality:" -ForegroundColor Yellow
    Write-Host "[ ] Long command output scrolls correctly" -ForegroundColor White
    Write-Host "[ ] Content preserved in scrollback buffer" -ForegroundColor White
    Write-Host "[ ] Manual scrollback navigation works" -ForegroundColor White
    Write-Host "[ ] Auto-scroll enables/disables correctly" -ForegroundColor White
    Write-Host "[ ] Viewport doesn't yank during history review" -ForegroundColor White
    Write-Host "[ ] Scrollback capacity management works" -ForegroundColor White
    Write-Host
    Write-Host "Resize Handling:" -ForegroundColor Yellow
    Write-Host "[ ] Content preserved during resize" -ForegroundColor White
    Write-Host "[ ] Cursor position remains valid after resize" -ForegroundColor White
    Write-Host "[ ] Scrollback accessible after resize" -ForegroundColor White
    Write-Host
    Write-Host "Advanced Features:" -ForegroundColor Yellow
    Write-Host "[ ] Mixed content types scroll properly" -ForegroundColor White
    Write-Host "[ ] Colors/formatting preserved in scrollback" -ForegroundColor White
    Write-Host "[ ] CSI scroll sequences work correctly" -ForegroundColor White
    Write-Host
    Write-Host "Performance:" -ForegroundColor Yellow
    Write-Host "[ ] High-volume output handled smoothly" -ForegroundColor White
    Write-Host "[ ] Terminal remains responsive during rapid output" -ForegroundColor White
    Write-Host "[ ] Memory usage remains stable" -ForegroundColor White
    Write-Host
    Write-Host "Application-Specific:" -ForegroundColor Yellow
    Write-Host "[ ] TestApp scrolling works correctly" -ForegroundColor White
    Write-Host "[ ] GameMod scrolling works correctly" -ForegroundColor White
    Write-Host "[ ] No integration issues with game/ImGui" -ForegroundColor White
    Write-Host
}

# Main execution
$overallSuccess = $true

# Run automated tests first
Write-Host "Step 1: Running Automated Tests" -ForegroundColor Magenta
Write-Host "===============================" -ForegroundColor Magenta
Write-Host

$automatedSuccess = Run-AutomatedTests
$propertySuccess = Run-PropertyTests

if (-not $automatedSuccess -or -not $propertySuccess) {
    $overallSuccess = $false
    Write-Host
    Write-Host "⚠️  Some automated tests failed. Manual validation is still recommended." -ForegroundColor Yellow
} else {
    Write-Host
    Write-Host "✓ All automated tests passed successfully!" -ForegroundColor Green
}

Write-Host
Write-Host "Step 2: Manual Validation" -ForegroundColor Magenta
Write-Host "=========================" -ForegroundColor Magenta
Write-Host

# Run application-specific tests
if ($All -or $TestApp) {
    $testAppSuccess = Test-TestApp
    if (-not $testAppSuccess) {
        $overallSuccess = $false
    }
}

if ($All -or $GameMod) {
    $gameModeSuccess = Test-GameMod
    if (-not $gameModeSuccess) {
        $overallSuccess = $false
    }
}

# Show validation checklist
Write-Host
Show-ValidationChecklist

# Final summary
Write-Host
Write-Host "Validation Summary:" -ForegroundColor Magenta
Write-Host "==================" -ForegroundColor Magenta

if ($overallSuccess) {
    Write-Host "✓ Automated tests: PASSED" -ForegroundColor Green
    Write-Host "⚠️  Manual validation: REQUIRED" -ForegroundColor Yellow
    Write-Host
    Write-Host "Next Steps:" -ForegroundColor Cyan
    Write-Host "1. Follow the manual testing instructions above" -ForegroundColor White
    Write-Host "2. Check off items in the validation checklist" -ForegroundColor White
    Write-Host "3. Document any issues in SCROLLING_VALIDATION_GUIDE.md" -ForegroundColor White
    Write-Host "4. Update task status once validation is complete" -ForegroundColor White
} else {
    Write-Host "✗ Some automated tests failed" -ForegroundColor Red
    Write-Host "⚠️  Manual validation still recommended" -ForegroundColor Yellow
    Write-Host
    Write-Host "Next Steps:" -ForegroundColor Cyan
    Write-Host "1. Review and fix failing automated tests" -ForegroundColor White
    Write-Host "2. Re-run this validation script" -ForegroundColor White
    Write-Host "3. Proceed with manual validation" -ForegroundColor White
}

Write-Host
Write-Host "For detailed validation instructions, see:" -ForegroundColor Gray
Write-Host "  SCROLLING_VALIDATION_GUIDE.md" -ForegroundColor Gray
Write-Host

exit $(if ($overallSuccess) { 0 } else { 1 })