$commands = bun generate_dotnet_test_filtered_commands.ts

foreach ($cmd in $commands) {
    if ([string]::IsNullOrWhiteSpace($cmd)) {
        continue
    }

    Write-Host "`n`n================================================================================" -ForegroundColor Cyan
    Write-Host "= START: $cmd" -ForegroundColor Cyan
    Write-Host "================================================================================" -ForegroundColor Cyan

    # Execute the command
    Invoke-Expression $cmd

    Write-Host "================================================================================" -ForegroundColor Cyan
    Write-Host "= END: $cmd" -ForegroundColor Cyan
    Write-Host "================================================================================`n`n" -ForegroundColor Cyan

}
