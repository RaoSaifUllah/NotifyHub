# Contributing
Read document/01-Requirements-and-Review.md through 04-Verification-and-Standards.md.
Use .NET SDK 10.0.401 and Node 24.11 or newer supported LTS.
Run dotnet tool restore, dotnet restore --locked-mode, and npm ci in Frontend.
Run dotnet build, dotnet test backend/tests/NotifyHub.UnitTests, npm run build,
and npm run test:e2e in Frontend. PostgreSQL integration tests require an isolated
database with migration, write and restricted read roles. Never use production data.
Keep Domain free of infrastructure; repositories do not commit independently.
Add authorization, tenant isolation and meaningful negative tests to changes.
Do not send real provider notifications during automated tests.
NotifyHub is licensed under AGPL-3.0-only; see LICENSE. The owner has authorized
source publication to the NotifyHub GitHub repository. External deployment and
live provider notifications require separate authorization.

## Local verification and publishing
Run meaningful checks locally before publishing changes.
Merge development branches into main locally, then push main directly.
Do not use GitHub Actions or action-based merge/deployment automation.
The authored CI workflow is archived in .github/disabled-workflows/ci.yml,
outside GitHub's active workflows directory.