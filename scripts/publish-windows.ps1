# Local Windows packaging (same flags as GitHub Actions).
# Usage: pwsh -File scripts/publish-windows.ps1
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$ver = "1.3.0"
$props = Join-Path $root "Directory.Build.props"
if (Test-Path $props) {
    $m = Select-String -Path $props -Pattern "<Version>([^<]+)</Version>" | Select-Object -First 1
    if ($m) { $ver = $m.Matches[0].Groups[1].Value }
}

$publishArgs = @(
    "-c", "Release",
    "-r", "win-x64",
    "--self-contained", "true",
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-p:EnableCompressionInSingleFile=true",
    "-p:DebugType=none"
)

dotnet publish (Join-Path $root "src/ILToCSConverter.App/ILToCSConverter.App.csproj") @publishArgs -o (Join-Path $root "artifacts/gui")
dotnet publish (Join-Path $root "src/ILToCSConverter.Cli/ILToCSConverter.Cli.csproj") @publishArgs -o (Join-Path $root "artifacts/cli")

$dist = Join-Path $root "artifacts/dist"
New-Item -ItemType Directory -Force -Path $dist | Out-Null
Compress-Archive -Path (Join-Path $root "artifacts/gui/*") -DestinationPath (Join-Path $dist "ILToCSConverter-$ver-win-x64.zip") -Force
Compress-Archive -Path (Join-Path $root "artifacts/cli/*") -DestinationPath (Join-Path $dist "ILToCS-$ver-win-x64.zip") -Force
Copy-Item (Join-Path $root "artifacts/gui/ILToCSConverter.exe") (Join-Path $dist "ILToCSConverter-$ver-win-x64.exe") -Force
Copy-Item (Join-Path $root "artifacts/cli/ILToCS.exe") (Join-Path $dist "ILToCS-$ver-win-x64.exe") -Force

Write-Host "Packages written to $dist"
Get-ChildItem $dist | Format-Table Name, Length
