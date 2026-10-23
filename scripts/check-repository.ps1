$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
$tracked = @(& git -C $project ls-files)
if ($LASTEXITCODE -ne 0 -or $tracked.Count -eq 0) { throw 'Run this check from a populated Git checkout.' }
$forbidden = @($tracked | Where-Object { $_ -match '(^|/)(build|test-runs|\.private|Recovery|StartupBackups)/|\.(exe|dll|pdb|zip|pfx|p12|pem|key|log)$|(^|/)(settings\.xml|\.env)(\.|$)' })
if ($forbidden.Count -gt 0) { throw ('Unexpected generated/private files are tracked: ' + ($forbidden -join ', ')) }
foreach ($required in @('README.md','LICENSE','SECURITY.md','CONTRIBUTING.md','src/Program.cs','src/SmartBoost.cs','tests/UiTests.cs','docs/images/QuietCare-preview.png','docs/images/QuietCare-monitor.png','docs/VALIDATION.md')) {
    if ($tracked -notcontains $required) { throw ('Required project file missing: ' + $required) }
}
& git -C $project diff --check
if ($LASTEXITCODE -ne 0) { throw 'Whitespace check failed.' }
Write-Output ('PASS Repository boundaries: ' + $tracked.Count + ' tracked files; required source/docs/images present; generated and private paths excluded.')
