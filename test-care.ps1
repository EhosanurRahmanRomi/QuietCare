param([switch]$Live)
$ErrorActionPreference = 'Stop'
$project = $PSScriptRoot
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
New-Item -ItemType Directory -Path (Join-Path $project 'build') -Force | Out-Null
$references = @('System.dll','System.Core.dll','System.Xml.dll','System.Xml.Linq.dll','System.Management.dll') | ForEach-Object { '/reference:' + (Join-Path $framework $_) }
$sources = @('Native.cs','Telemetry.cs','Storage.cs','Startup.cs','CareScan.cs') | ForEach-Object { Join-Path (Join-Path $project 'src') $_ }
& (Join-Path $framework 'csc.exe') /nologo /target:exe /platform:x64 /optimize+ /warn:4 /warnaserror+ "/out:$project\build\CareTests.exe" @references @sources (Join-Path $project 'tests\CareTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Care test compilation failed.' }
$fixture = Join-Path $project ('test-runs\care-' + [Guid]::NewGuid().ToString('N'))
if ($Live) { & (Join-Path $project 'build\CareTests.exe') $fixture --live | Tee-Object -FilePath (Join-Path $project 'build\care-live-results.txt') }
else { & (Join-Path $project 'build\CareTests.exe') $fixture | Tee-Object -FilePath (Join-Path $project 'build\care-test-results.txt') }
if ($LASTEXITCODE -ne 0) { throw 'Care tests failed.' }
