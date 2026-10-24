param(
    [string]$Output = (Join-Path $PSScriptRoot '../artifacts/installer'),
    [string]$Python = 'python',
    [string]$PublishedApp
)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$outputPath = [IO.Path]::GetFullPath($Output)
[IO.Directory]::CreateDirectory($outputPath) | Out-Null
& $Python -c 'import msilib,sys; assert sys.version_info[:2] == (3,12), "Python 3.12 is required for the MSI build tool."'
if ($LASTEXITCODE -ne 0) { throw 'Python 3.12 Windows MSI library is required for packaging only.' }
if (!$PublishedApp) {
    $PublishedApp = Join-Path $outputPath 'published'
    & (Join-Path $root 'build.ps1') -Output $PublishedApp
}
$appPath = (Resolve-Path -LiteralPath $PublishedApp).Path
if (!(Test-Path -LiteralPath (Join-Path $appPath 'QuietCare.Next.exe'))) { throw 'Published application is missing.' }
$payload = Join-Path $outputPath 'payload'
[IO.Directory]::CreateDirectory($payload) | Out-Null
Copy-Item -LiteralPath (Join-Path $appPath 'QuietCare.Next.exe') -Destination $payload
foreach ($name in @('LICENSE','THIRD-PARTY-NOTICES.md')) {
    Copy-Item -LiteralPath (Join-Path $root $name) -Destination $payload
}
Copy-Item -LiteralPath (Join-Path $root 'src/Cleaner.App/Assets/QuietCare.ico') -Destination $payload
Copy-Item -LiteralPath (Join-Path $root 'docs/INSTALLATION.md') -Destination (Join-Path $payload 'INSTALLATION.md')
Copy-Item -LiteralPath (Join-Path $root 'licenses') -Destination $payload -Recurse -Force
[xml]$project = Get-Content -LiteralPath (Join-Path $root 'src/Cleaner.App/Cleaner.App.csproj')
$version = ($project.Project.PropertyGroup.Version | Where-Object { $_ }) -replace '-.*$',''
$package = Join-Path $outputPath 'QuietCare.msi'
& $Python (Join-Path $PSScriptRoot 'build_msi.py') --payload $payload --output $package --version $version
if ($LASTEXITCODE -ne 0) { throw 'MSI authoring failed.' }
& (Join-Path $PSScriptRoot 'build-setup.ps1') -Package $package -Output (Join-Path $outputPath 'QuietCare-Setup.exe')
if ($LASTEXITCODE -ne 0) { throw 'Setup EXE compilation failed.' }
Write-Output "Built native MSI and setup EXE in $outputPath"
