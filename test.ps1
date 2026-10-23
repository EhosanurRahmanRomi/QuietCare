$ErrorActionPreference = 'Stop'
$project = $PSScriptRoot
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$fixture = Join-Path $project ('test-runs\' + [Guid]::NewGuid().ToString('N'))
$tempRoot = Join-Path $fixture 'Temp'
$outside = Join-Path $fixture 'outside'
New-Item -ItemType Directory -Force -Path $tempRoot,$outside | Out-Null
$outsideFile = Join-Path $outside 'outside.tmp'
Set-Content -LiteralPath $outsideFile -Value 'Outside the cleanup scope'
[System.IO.File]::SetLastWriteTimeUtc($outsideFile,[DateTime]::UtcNow.AddDays(-10))
New-Item -ItemType Junction -Path (Join-Path $tempRoot 'junction') -Target $outside | Out-Null
$references = @('System.Net.Http.dll','System.Web.Extensions.dll','System.dll','System.Core.dll','System.Xml.dll','System.Xml.Linq.dll','System.Management.dll') | ForEach-Object { '/reference:' + (Join-Path $framework $_) }
$sources = @('Native.cs','Telemetry.cs','Storage.cs','Startup.cs','Tools.cs','AudioActivity.cs','SmartBoost.cs','CareScan.cs','Updates.cs') | ForEach-Object { Join-Path (Join-Path $project 'src') $_ }
& (Join-Path $framework 'csc.exe') /nologo /target:exe /platform:x64 /optimize+ /warn:4 /warnaserror+ "/out:$project\build\MemoryFixture.exe" (Join-Path $project 'tests\MemoryFixture.cs')
if ($LASTEXITCODE -ne 0) { throw 'Memory fixture compilation failed.' }
& (Join-Path $framework 'csc.exe') /nologo /target:exe /platform:x64 /optimize+ /warn:4 /warnaserror+ "/out:$project\build\CoreTests.exe" @references @sources (Join-Path $project 'tests\CoreTests.cs') (Join-Path $project 'tests\AdvancedTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
& (Join-Path $project 'build\CoreTests.exe') $fixture | Tee-Object -FilePath (Join-Path $project 'build\test-results.txt')
if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
