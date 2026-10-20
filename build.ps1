<#
.SYNOPSIS
    Builds Gridiron Front Office and runs its gates. Run it before every commit.

.DESCRIPTION
    Default: restore -> build (warnings as errors) -> fast tests (Category!=Slow), including the architecture
    rules and, from M0 WP6, the transaction torture tests.

    -Full adds the slow gates: slow tests, then a Release ReadyToRun publish of the desktop app into ./artifacts.
    Later milestones add SimLab calibration (M2), E2E smoke tests (M3) and the 30-season autoplay (M5).
    CI doesn't exist yet (F-30). When it does, it will call this script.

.EXAMPLE
    ./build.ps1
.EXAMPLE
    ./build.ps1 -Full
#>
[CmdletBinding()]
param(
    [switch] $Full,
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$solution = Join-Path $root 'GridironFrontOffice.slnx'
$artifacts = Join-Path $root 'artifacts'
$started = Get-Date

function Invoke-Step {
    param([string] $Name, [scriptblock] $Command)
    Write-Host ""
    Write-Host "==> $Name" -ForegroundColor Cyan
    & $Command
    if ($LASTEXITCODE -ne 0) {
        Write-Host "FAILED: $Name (exit code $LASTEXITCODE)" -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

# Tests run on Microsoft.Testing.Platform (global.json "test.runner"); filters are xUnit v3 trait filters.
# Exit code 8 means "zero tests ran" in a project. That's expected for test projects that are still empty
# and for projects with no [Trait("Category", "Slow")] tests, so it's the one non-zero code we accept.
$testArgs = @('--solution', $solution, '--configuration', $Configuration, '--no-build', '--ignore-exit-code', '8')

Invoke-Step 'Restore' { dotnet restore $solution }
Invoke-Step "Build ($Configuration, warnings as errors)" { dotnet build $solution --configuration $Configuration --no-restore -warnaserror }
Invoke-Step 'Fast tests (Category!=Slow)' { dotnet test @testArgs --filter-not-trait 'Category=Slow' }

if ($Full) {
    Invoke-Step 'Slow tests (Category=Slow)' { dotnet test @testArgs --filter-trait 'Category=Slow' }

    $publishDir = Join-Path $artifacts 'publish/desktop'
    Invoke-Step 'Publish desktop (Release, win-x64, self-contained, ReadyToRun)' {
        dotnet publish (Join-Path $root 'src/Desktop/GridironFrontOffice.Desktop.csproj') `
            --configuration Release --runtime win-x64 --self-contained `
            -p:PublishReadyToRun=true --output $publishDir
    }
}

$elapsed = (Get-Date) - $started
Write-Host ""
Write-Host ("All gates passed in {0:mm\:ss}." -f $elapsed) -ForegroundColor Green
