$ErrorActionPreference = 'Stop'
$project = $PSScriptRoot
& (Join-Path $project 'build.ps1')
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$payload = Join-Path $project 'build\QuietCare.exe'
$hash = (Get-FileHash -LiteralPath $payload -Algorithm SHA256).Hash
$setupSource = [IO.File]::ReadAllText((Join-Path $project 'installer\Setup.cs')).Replace('PAYLOAD_HASH',$hash)
$generated = Join-Path $project 'build\Setup.generated.cs'
[IO.File]::WriteAllText($generated,$setupSource,[Text.UTF8Encoding]::new($false))
$references = @('System.dll','System.Core.dll','System.Xml.dll','System.Xml.Linq.dll','System.Drawing.dll','System.Windows.Forms.dll') | ForEach-Object { '/reference:' + (Join-Path $framework $_) }
& (Join-Path $framework 'csc.exe') /nologo /target:winexe /platform:x64 /optimize+ /warn:4 /warnaserror+ "/win32manifest:$project\app.manifest" "/win32icon:$project\quietcare.ico" "/resource:$payload,QuietCarePayload" "/out:$project\build\QuietCare-Setup.exe" @references $generated (Join-Path $project 'src\Native.cs') (Join-Path $project 'src\Storage.cs') (Join-Path $project 'src\Telemetry.cs')
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
Get-FileHash -LiteralPath $payload,(Join-Path $project 'build\QuietCare-Setup.exe') -Algorithm SHA256
