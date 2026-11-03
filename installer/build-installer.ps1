# Builds publish\installer\League-Auto-Accept-Setup-<version>.msi
# The app is published self-contained, so the PC it's installed on doesn't need .NET.
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$project = Join-Path $repo 'Leauge Auto Accept\Leauge Auto Accept.csproj'
$out = Join-Path $repo 'publish\installer'
$payload = Join-Path $out 'app'
$wixVersion = '5.0.2'

if (Test-Path $out) { Remove-Item $out -Recurse -Force }
New-Item -ItemType Directory $out | Out-Null

function Run($exe) {
    & $exe @args
    if ($LASTEXITCODE -ne 0) { throw "$exe failed with exit code $LASTEXITCODE" }
}

$version = ([xml](Get-Content $project)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1

Run dotnet publish $project -c Release -r win-x64 --self-contained true -o $payload -v q `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true

# The installer shows the license as rich text
$license = (Get-Content (Join-Path $repo 'LICENSE') -Raw).Replace('\', '\\').Replace('{', '\{').Replace('}', '\}') -replace "\r?\n", "\par`r`n"
$licensePath = Join-Path $out 'license.rtf'
Set-Content $licensePath -Encoding Ascii -Value ("{\rtf1\ansi\deff0{\fonttbl{\f0 Segoe UI;}}\fs18`r`n" + $license + "}")

Push-Location $repo
try {
    Run dotnet tool restore
    Run dotnet wix extension add -g "WixToolset.UI.wixext/$wixVersion"
    Run dotnet wix build (Join-Path $PSScriptRoot 'Product.wxs') `
        -ext "WixToolset.UI.wixext/$wixVersion" `
        -d "Version=$version" -d "PayloadDir=$payload" -d "LicensePath=$licensePath" `
        -d "IconPath=$(Join-Path $repo 'Leauge Auto Accept\icon.ico')" `
        -arch x64 -o (Join-Path $out "League-Auto-Accept-Setup-$version.msi")
}
finally {
    Pop-Location
}

Write-Host "Built $(Join-Path $out "League-Auto-Accept-Setup-$version.msi")"
