# StressLab Performance Test Runner
# PowerShell script for easy test execution

param(
    [Parameter(Mandatory=$false)]
    [string]$Scenario = "",
    
    [Parameter(Mandatory=$false)]
    [int]$Duration = 60,
    
    [Parameter(Mandatory=$false)]
    [int]$Users = 10,
    
    [Parameter(Mandatory=$false)]
    [string]$Environment = "Development",
    
    [Parameter(Mandatory=$false)]
    [string]$ScenariosFile = "scenarios.json",
    
    [Parameter(Mandatory=$false)]
    [switch]$ListScenarios,
    
    [Parameter(Mandatory=$false)]
    [switch]$Help
)

# Display help information
if ($Help) {
    Write-Host "StressLab Performance Test Runner" -ForegroundColor Green
    Write-Host "=================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Usage:" -ForegroundColor Yellow
    Write-Host "  .\Run-Test.ps1 -Scenario 'API Performance Test' -Duration 120 -Users 50"
    Write-Host ""
    Write-Host "Parameters:" -ForegroundColor Yellow
    Write-Host "  -Scenario      Name of the scenario to execute"
    Write-Host "  -Duration      Test duration in seconds (default: 60)"
    Write-Host "  -Users         Number of concurrent users (default: 10)"
    Write-Host "  -Environment   Environment (Development/Production) (default: Development)"
    Write-Host "  -ScenariosFile Path to scenarios JSON file (default: scenarios.json)"
    Write-Host "  -ListScenarios List all available scenarios"
    Write-Host "  -Help          Show this help message"
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Yellow
    Write-Host "  .\Run-Test.ps1 -ListScenarios"
    Write-Host "  .\Run-Test.ps1 -Scenario 'API Performance Test'"
    Write-Host "  .\Run-Test.ps1 -Scenario 'Database Performance Test' -Duration 300 -Users 100"
    Write-Host "  .\Run-Test.ps1 -Scenario 'System Impact Assessment' -Environment Production"
    Write-Host ""
    exit 0
}

# Set environment variable
$env:ASPNETCORE_ENVIRONMENT = $Environment

# Build the command
$command = "dotnet run --project tests/StressLab.PerformanceTests"

# Add parameters
if ($ListScenarios) {
    $command += " -- --list-scenarios"
} elseif ($Scenario) {
    $command += " -- --scenario `"$Scenario`""
    $command += " --duration $Duration"
    $command += " --users $Users"
} else {
    $command += " -- --duration $Duration --users $Users"
}

# Display execution information
Write-Host "StressLab Performance Test Runner" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green
Write-Host ""
Write-Host "Environment: $Environment" -ForegroundColor Cyan
Write-Host "Scenarios File: $ScenariosFile" -ForegroundColor Cyan

if ($ListScenarios) {
    Write-Host "Action: List available scenarios" -ForegroundColor Cyan
} elseif ($Scenario) {
    Write-Host "Scenario: $Scenario" -ForegroundColor Cyan
    Write-Host "Duration: $Duration seconds" -ForegroundColor Cyan
    Write-Host "Users: $Users" -ForegroundColor Cyan
} else {
    Write-Host "Action: Run default test" -ForegroundColor Cyan
    Write-Host "Duration: $Duration seconds" -ForegroundColor Cyan
    Write-Host "Users: $Users" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "Command: $command" -ForegroundColor Yellow
Write-Host ""

# Execute the command
try {
    Invoke-Expression $command
    Write-Host ""
    Write-Host "Test execution completed successfully!" -ForegroundColor Green
} catch {
    Write-Host ""
    Write-Host "Test execution failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Display report locations
$reportsPath = "tests\StressLab.PerformanceTests\reports"
if (Test-Path $reportsPath) {
    Write-Host ""
    Write-Host "Reports generated in: $reportsPath" -ForegroundColor Green
    Write-Host "Available reports:" -ForegroundColor Green
    
    $reports = Get-ChildItem $reportsPath -Filter "*.html" | Sort-Object LastWriteTime -Descending
    foreach ($report in $reports) {
        Write-Host "  - $($report.Name)" -ForegroundColor White
    }
}

