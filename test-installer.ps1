$ErrorActionPreference = 'Stop'
$project = $PSScriptRoot
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$fixture = Join-Path $project ('test-runs\installer-' + [Guid]::NewGuid().ToString('N'))
$env:QUIETCARE_INSTALL_TEST = $fixture
$references = @('System.dll','System.Core.dll','System.Xml.dll','System.Xml.Linq.dll','System.Drawing.dll','System.Windows.Forms.dll') | ForEach-Object { '/reference:' + (Join-Path $framework $_) }
$payload = Join-Path $project 'build\QuietCare.exe'
$generated = Join-Path $project 'build\Setup.generated.cs'
& (Join-Path $framework 'csc.exe') /nologo /define:INSTALLER_TEST /main:InstallerTests /target:exe /platform:x64 /optimize+ /warn:4 /warnaserror+ "/resource:$payload,QuietCarePayload" "/out:$project\build\InstallerTests.exe" @references $generated (Join-Path $project 'src\Native.cs') (Join-Path $project 'src\Storage.cs') (Join-Path $project 'src\Telemetry.cs') (Join-Path $project 'tests\InstallerTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Installer test compilation failed.' }
try {
    & (Join-Path $project 'build\InstallerTests.exe') | Tee-Object -FilePath (Join-Path $project 'build\installer-test-results.txt')
    if ($LASTEXITCODE -ne 0) { throw 'Installer tests failed.' }
} finally { Remove-Item Env:\QUIETCARE_INSTALL_TEST }
