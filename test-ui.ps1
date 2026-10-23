$ErrorActionPreference = 'Stop'
$project = $PSScriptRoot
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$references = @('System.dll','System.Core.dll','System.Xml.dll','System.Xml.Linq.dll','System.Management.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Xaml.dll') | ForEach-Object { '/reference:' + (Join-Path $framework $_) }
$references += @('WindowsBase.dll','PresentationCore.dll','PresentationFramework.dll') | ForEach-Object { '/reference:' + (Join-Path (Join-Path $framework 'WPF') $_) }
$sources = Get-ChildItem -LiteralPath (Join-Path $project 'src') -Filter '*.cs' | ForEach-Object FullName
$fixture = Join-Path $project ('test-runs\ui-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fixture -Force | Out-Null
& (Join-Path $framework 'csc.exe') /nologo /main:UiTests /target:exe /platform:x64 /optimize+ /warn:4 /warnaserror+ "/out:$project\build\UiTests.exe" @references @sources (Join-Path $project 'tests\UiTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'UI test compilation failed.' }
& (Join-Path $project 'build\UiTests.exe') $fixture | Tee-Object -FilePath (Join-Path $project 'build\ui-test-results.txt')
if ($LASTEXITCODE -ne 0) { throw 'UI tests failed.' }
