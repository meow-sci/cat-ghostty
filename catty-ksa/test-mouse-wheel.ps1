#!/usr/bin/env pwsh

Write-Host "Testing mouse wheel scrolling in caTTY TestApp"
Write-Host "=============================================="
Write-Host ""
Write-Host "Instructions:"
Write-Host "1. The TestApp should open with a terminal window"
Write-Host "2. Run some commands to generate content (e.g., 'dir', 'ls', 'help')"
Write-Host "3. Try scrolling with your mouse wheel over the terminal"
Write-Host "4. You should now see the content scroll up and down"
Write-Host "5. Press Ctrl+C in the terminal to exit"
Write-Host ""
Write-Host "Starting TestApp..."

Set-Location "caTTY.TestApp"
dotnet run