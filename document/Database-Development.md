# PostgreSQL development gate
The owner supplied an isolated PostgreSQL 18.6 development database.
The initial migration is applied. Restricted notifyhub_write and notifyhub_read
roles were generated with random passwords stored only in the ignored local file.
The owner migration credential was exposed by an initial parser exception:
the owner confirmed rotation and the updated Migration credential is secured in the ignored file.

Use backend/src/NotifyHub.Api/appsettings.Local.json for local credentials.
Write, Read and Migration must be distinct. Never paste credentials in chat.
PostgreSQL URLs are converted to Npgsql format with verified TLS, and parser
exceptions are suppressed. Environment variables override the local file.

## Migrations and grants
Use a direct PostgreSQL connection for schema operations; transaction poolers
may not preserve session settings. Explicit migration command with
ConnectionStrings__Migration configured:

    dotnet ef database update --project backend/src/NotifyHub.Infrastructure

Run deploy/database-roles.sql as schema owner after migrations.
Read can select only approved projection views, with no underlying-table access.
Write needs SELECT on migration history for startup/readiness compatibility.
No broad default grants to future tables.

Development helper (from NotifyHub root):

    dotnet run --project scripts/NotifyHub.Database
    dotnet run --project scripts/NotifyHub.Database -- migrate
    dotnet run --project scripts/NotifyHub.Database -- grants

The setup mode provisions roles for a fresh isolated installation only. It refuses
to replace existing roles/passwords. It never echoes credentials. Backup secrets
through restricted operator tooling; the helper is not a production secret manager.

## Integration tests
Run from NotifyHub root:

    ./scripts/Test-PostgreSql.ps1

The runner loads ignored configuration into process-local test environment variables
and removes them afterwards. Tests create one UUID-named notifyhub_test_* schema,
apply migrations with a schema-specific migration history, grant limited access,
verify rollback and scoped projections, then drop only the schema created by that run.
Existing databases/schemas are never dropped. PostgreSQL CREATE SCHEMA privilege is
required for the migration test role. Use an isolated development/test database.
For the supplied hosted service, tests select its direct endpoint from the pooler
hostname. A separate explicit test connection is preferable in future CI.
Missing configuration fails, and SQLite is never substituted.

## Identity key ring
Authenticator keys are encrypted with ASP.NET Data Protection and bound to the
account ID. Recovery codes are generated with high entropy and stored as hashes.
Development keys are under the host output secrets/identity-keys directory (ignored).
Set Security__IdentityKeyDirectory to a stable, restricted directory for deployment.
The development key ring is not encrypted at rest; production key protection,
permissions, backup/restore and rotation must be completed before release.
Losing the key ring prevents authenticator-secret decryption. Back it up separately.
The current restore test only reopens the local key ring; it is not the full isolated
production restore drill required by Phase 6.
