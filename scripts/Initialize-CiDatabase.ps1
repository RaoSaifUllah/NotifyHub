# Disposable CI database only. Do not run against production.
$ErrorActionPreference = 'Stop'
$connection = 'Host=127.0.0.1;Port=5432;Database=notifyhub_ci;Username=postgres;Password=ci-disposable-only'
$localPath = 'backend/src/NotifyHub.Api/appsettings.Local.json'
if (Test-Path -LiteralPath $localPath) { throw 'Refusing to replace existing local database settings.' }
@{ ConnectionStrings = @{ Write = $connection; Read = $connection; Migration = $connection } } |
    ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $localPath
dotnet run --project scripts/NotifyHub.Database -- setup
if ($LASTEXITCODE -ne 0) { throw 'CI database setup failed.' }
