# Збирання пакета випуску з чистої копії перевіреного коміту (ЛР №4, крок 5.9).
# Використання (з кореня репозиторію): powershell -ExecutionPolicy Bypass -File scripts/release.ps1 -Version 1.0.0
param(
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$out = Join-Path $root "artifacts/v$Version"
$archive = Join-Path $root "artifacts/tuning-shop-v$Version.zip"

if (git -C $root status --porcelain) {
    throw "Робоча копія містить незафіксовані зміни. Пакет збирається лише з чистого коміту."
}
$commit = git -C $root rev-parse HEAD
Write-Host "Коміт: $commit"

if (Test-Path $out) { Remove-Item -Recurse -Force $out }
if (Test-Path $archive) { Remove-Item -Force $archive }

# 1. Тести на тому самому коміті.
dotnet test (Join-Path $root "backend/TuningShop.sln") -c Release
if ($LASTEXITCODE -ne 0) { throw "Тести не пройдено." }

# 2. Клієнт: статичний експорт Next.js.
Push-Location (Join-Path $root "frontend")
npm ci
if ($LASTEXITCODE -ne 0) { throw "npm ci завершився з помилкою." }
npm run build
if ($LASTEXITCODE -ne 0) { throw "Збирання клієнта завершилося з помилкою." }
Pop-Location

# 3. Сервер разом із клієнтом у wwwroot.
dotnet publish (Join-Path $root "backend/src/TuningShop.Api/TuningShop.Api.csproj") -c Release -o $out
if ($LASTEXITCODE -ne 0) { throw "Публікація сервера завершилася з помилкою." }
Copy-Item -Recurse (Join-Path $root "frontend/out") (Join-Path $out "wwwroot")

# 4. Інструкція запуску та відомості про походження пакета.
Copy-Item (Join-Path $root "docs/releases/RUN.md") (Join-Path $out "RUN.md")
"version=$Version`ncommit=$commit`nbuilt=$((Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ'))" |
    Set-Content -Encoding utf8 (Join-Path $out "BUILD-INFO.txt")

# 5. Архів і контрольна сума.
Compress-Archive -Path (Join-Path $out "*") -DestinationPath $archive
$hash = (Get-FileHash -Algorithm SHA256 $archive).Hash.ToLowerInvariant()
"$hash  tuning-shop-v$Version.zip" | Set-Content -Encoding ascii "$archive.sha256"

Write-Host "Пакет: $archive"
Write-Host "SHA-256: $hash"
