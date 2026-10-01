# Run from the NotifyHub root. Credentials are read locally and never echoed.
$configuration = Get-Content backend/src/NotifyHub.Api/appsettings.Local.json -Raw | ConvertFrom-Json
try {
    $env:NOTIFYHUB_TEST_MIGRATION = $configuration.ConnectionStrings.Migration
    $env:NOTIFYHUB_TEST_WRITE = $configuration.ConnectionStrings.Write
    $env:NOTIFYHUB_TEST_READ = $configuration.ConnectionStrings.Read
    dotnet test backend/tests/NotifyHub.IntegrationTests --verbosity quiet --logger "console;verbosity=normal"
    if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL tests failed.' }
} finally {
    Remove-Item Env:NOTIFYHUB_TEST_MIGRATION -ErrorAction SilentlyContinue
    Remove-Item Env:NOTIFYHUB_TEST_WRITE -ErrorAction SilentlyContinue
    Remove-Item Env:NOTIFYHUB_TEST_READ -ErrorAction SilentlyContinue
}


