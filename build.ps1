param([string]$Output = (Join-Path $PSScriptRoot 'dist'))
$ErrorActionPreference = 'Stop'
$expected = '2B4B128D8D510CC2D442693CF848C050D700D43DC667AFE9E5FD82B0570D667E'
if ((Get-FileHash (Join-Path $PSScriptRoot 'native/SDL3.dll') -Algorithm SHA256).Hash -ne $expected) { throw 'The bundled SDL runtime checksum does not match.' }
dotnet publish (Join-Path $PSScriptRoot 'PadGlass.csproj') -c Release -o $Output
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
Copy-Item (Join-Path $PSScriptRoot 'native/SDL-LICENSE.txt') $Output
Copy-Item (Join-Path $PSScriptRoot 'THIRD-PARTY-NOTICES.txt') $Output
Copy-Item (Join-Path $PSScriptRoot 'LICENSE') (Join-Path $Output 'PadGlass-LICENSE.txt')
$exe = Join-Path $Output 'PadGlass.exe'
$hash = (Get-FileHash $exe -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -Path (Join-Path $Output 'SHA256SUMS.txt') -Value "$hash  PadGlass.exe" -Encoding ascii
Write-Host "Built $exe"
