# Implementation status
Updated: 2026-10-01 (Asia/Karachi). NotifyHub v1 is incomplete.
Percentages are estimates of implementation scope, not coverage, certification or assurance.

## Milestone progress
| Phase | State | Done | Pending |
|---|---|---:|---:|
| 0 Baseline and decisions | Verified local skeleton exit checks | 100% | 0% |
| 1 Architecture and persistence | Verified foundation exit checks | 100% | 0% |
| 2 Identity and workspaces | In progress; backend foundation tested | 42% | 58% |
| 3 Applications, keys and ingestion | Not started | 0% | 100% |
| 4 Routing and providers | Not started | 0% | 100% |
| 5 Complete dashboard | Feature work not started; foundation UI exists | 0% | 100% |
| 6 Operational hardening | Not started; baseline observability/CI exists | 0% | 100% |
| 7 Release handoff | Not started | 0% | 100% |

Overall v1 estimate: **15% implemented, 85% pending**. Phases have unequal size.

## Module progress
| Module | Done | Pending | Implemented / remaining |
|---|---:|---:|---|
| Tooling, solution, dependency locks | 100% | 0% | Five production layers, DB helper, tests, SDK and package locks |
| Architecture / persistence foundation | 100% | 0% | EF repositories/UoW, scoped Dapper view, migrations, actual PostgreSQL checks |
| Accounts / JWT / refresh / MFA / RBAC | 48% | 52% | Identity/Argon2id, JWT issuer, rotating session services, protected MFA store tested; public lifecycle and enforcement/UI pending |
| Workspaces / membership | 25% | 75% | Persistence, permission rules, locking and last-owner use case; invitations/APIs/step-up pending |
| Applications / scoped keys | 0% | 100% | Not implemented |
| Ingestion / idempotency / queue / retries | 0% | 100% | Not implemented; worker is a composition root only |
| Routing / templates / quiet hours | 0% | 100% | Not implemented |
| Email / Telegram / Discord / webhook / Web Push | 0% | 100% | Not implemented; no notifications sent |
| Dashboard / history / replay | 12% | 88% | Modern responsive themed/routed baseline; authenticated feature screens/data pending |
| Provider encryption / SSRF / audit / retention | 0% | 100% | Not implemented; identity secret protection is counted above |
| Observability / deployment / restore | 10% | 90% | JSON logs, health/readiness, redacted errors; Compose/metrics/runbooks/full restore pending |
| Verification / CI / documentation | 20% | 80% | Local suites and authored PG CI; complete journeys, scans, load/release evidence pending |

## Actual implemented behavior
- Domain/Application dependency boundaries; independent API/worker composition roots.
- Scoped aggregate repositories and one command-owned Unit of Work.
- PostgreSQL migrations: InitialWorkspace, IdentityFoundation, RotatingSessions.
- Read role gets a reviewed workspace projection view only; Write uses explicit grants.
- Restricted logins provisioned; credentials live only in ignored appsettings.Local.json.
- ASP.NET Identity stores disable AutoSaveChanges; normalized email uniqueness.
- Argon2id via Konscious 1.3.1: 64MiB/t=3/p=1, random salts, constant-time digest comparison,
  bounded stored parameters, rehash support and two concurrent hashing work slots.
- Workspace policy denies read-only writes and developer membership changes.
  Membership command locks workspace before checking owners.
- Refresh/CSRF secrets are opaque 256-bit values; database stores SHA-256 hashes.
  Family row lock serializes rotations; token is reloaded after lock.
  Replay revocation commits on rejected refresh; no reusable grace window.
  Sessions have seven-day idle and thirty-day absolute limits.
- Five-minute RS256 issuer with RSA >=3072 bits and kid fingerprint.
  API bearer validation enforces RS256, issuer/audience/type, expiry and current active
  session. /api/v1/auth/session is protected; absent keys and storage failures deny access.
  Public login, enrollment and refresh-cookie routes remain pending.
- TOTP secrets encrypted through ASP.NET Data Protection with account-bound purpose.
  High-entropy recovery code hashes redeem atomically while locking the account.
  Enrollment, login, step-up and recovery HTTP flows/UI are still pending.
- /health, /ready, development /openapi/v1.json, correlation IDs and safe Problem Details.
- Modern light/dark UI: restrained violet accent, vector icons, sidebar, polished empty
  state, theme preference, mobile layout, reduced motion, loading/error/retry and 404 recovery.
  Visible copy externalized. No authentication tokens persisted by the frontend.

## Verification environment
Windows; .NET SDK 10.0.401/runtime 10.0.12; Node 24.11.0/npm 11.6.1;
owner-supplied isolated PostgreSQL 18.6; Playwright Chromium.
No Docker installed. No remote CI executed.

## Commands and actual results
| Command | Result |
|---|---|
| dotnet restore NotifyHub.slnx --locked-mode --verbosity quiet | PASS |
| dotnet build NotifyHub.slnx --no-restore --verbosity quiet | PASS, zero warnings/errors |
| dotnet test backend/tests/NotifyHub.UnitTests --verbosity quiet | PASS, 20/20 |
| ./scripts/Test-PostgreSql.ps1 | PASS, 8/8 including the final real PostgreSQL repeat |
| dotnet ef migrations has-pending-model-changes --project backend/src/NotifyHub.Infrastructure | PASS, no pending model changes |
| dotnet ef migrations script --idempotent --project backend/src/NotifyHub.Infrastructure --output deploy/migrations.sql | PASS |
| dotnet run --project scripts/NotifyHub.Database -- migrate | PASS, all three migrations applied |
| dotnet run --project scripts/NotifyHub.Database -- grants | PASS, reviewed grants applied |
| dotnet test backend/tests/NotifyHub.IntegrationTests --filter "FullyQualifiedName~AccessAuthenticationTests&#124;FullyQualifiedName~ApiFoundationTests&#124;FullyQualifiedName~ErrorRedactionTests" --verbosity quiet | PASS, 17/17 HTTP checks (10 new JWT/session cases) |
| npm run build --prefix Frontend | PASS |
| npm run test:e2e --prefix Frontend | PASS, 5/5 |
| npm install / dependency audit | PASS, zero reported npm vulnerabilities |
| dotnet list NotifyHub.slnx package --vulnerable --include-transitive | PASS, no reported vulnerable packages across all eight projects |

PostgreSQL fixture uses one generated UUID schema per run, explicit schema migration history,
held connections with parameterized set_config, safely quoted Dapper schema identifiers,
then removes only the schema it created. Existing databases/schemas are never dropped.
The one PostgreSQL test contains multiple meaningful assertions; count is not coverage.

Covered checks:
- Empty-schema migrations, rollback after EF flush, workspace-scoped Dapper projection.
- Read role cannot write or read raw workspaces, users, refresh sessions or refresh tokens.
- Identity no-autosave, normalized email uniqueness, Argon2id hash storage/verification.
- Last-owner demotion and unauthorized membership mutation rejected.
- CSRF mismatch, refresh rotation, replay/family revoke, concurrent refresh race,
  logout revoke and password/security-stamp invalidation.
- Session idle/absolute boundaries; permanent revocation.
- JWT issuer/audience/algorithm/expiry/signature tamper and weak-key rejection.
- MFA ciphertext, independent Otp.NET 1.4.1 interoperability, local key-ring reopening,
  hashed recovery storage and recovery-code reuse rejection.
- API health independent of DB outage, failed readiness, OpenAPI and typed/redacted errors.
- Playwright health retry, 390px viewport, keyboard skip link, persisted light/dark theme,
  missing-route recovery, and axe WCAG-tagged checks in both themes with no reported violations.

Initial Argon2 timing: 1019ms for one hash on a host reporting 80 logical processors.
This is an initial measurement, not a reference load/performance result.
Automated axe checks are not manual WCAG/screen-reader verification or certification.
Screenshots in document/screenshots use mocked health responses, without fake delivery activity.

## Resolved problems and security incident
- Initial writes were denied by Windows execution permissions; approved shell wrote only
  NotifyHub project files. No automatic approval rejection occurred.
- EF transitive 10.0.0/Design 10.0.12 conflict fixed with explicit EF/Relational 10.0.12 pins.
- Missing Vite CSS types fixed with vite-env.d.ts.
- Initial database preflight parsed a PostgreSQL URL outside its exception boundary and
  printed an owner credential in tool output. Owner was informed and confirmed password
  rotation. Updated credential secured in ignored local settings and reconnection verified.
  Parser failures now suppress input/inner exceptions; regression test passes.
- Schema isolation initially failed because search_path alone did not isolate migration
  history and projections. Explicit history schema, set_config and quoted projection
  schema fixed it; real PostgreSQL fixture now passes.
- JWT test found signer caching a disposed per-call RSA object. Disabled that cache;
  repeat signing and all negative JWT tests pass.
- A build once overlapped the API smoke process and emitted file-lock retry warnings.
  API stopped; final rebuild passed with zero warnings/errors.

## Unresolved work / next steps
1. Complete Phase 2: one-time bootstrap, registration/invite mode, public JWT validation
   configuration/bootstrap integration, exact Origin/CSRF and secure rotating cookie endpoints,
   lockout/rate limits, email verify/reset mocks and single-use challenges, MFA/recovery
   flows/step-up, immutable audit, workspace/member/session APIs and frontend journeys.
2. Verify account/session APIs and cross-workspace policies before Phase 3.
3. Then proceed Phase 3 through Phase 7 in documented order.
4. Development Data Protection keys are in ignored output/secrets. Production needs
   a stable restricted key directory, encryption/protection policy and full restore drill.
   Local key-ring reopening is not the required production backup/restore verification.
5. Provider contracts, full notification Playwright journey, browser/screen-reader review,
   SAST/secret/container scans, SBOM, reference load, Compose install/upgrade and restore
   remain unexecuted. ASVS control-by-control evidence is not complete.
6. Docker is needed for later Compose gates. Hosted CI is authored with postgres:18.6,
   but unexecuted. Supply-chain action SHA pinning/scanning remains Phase 6 work.
7. Git repository URL and AGPL-3.0-only confirmation are required before publication.
   Owner subsequently requested the GitHub source push. Git initialized only inside
   NotifyHub and origin configured; owner renewed source-push authorization while requesting a license explanation.
   No external deployment or live notifications occurred.

## ADRs and changed areas
ADR-001 baseline decisions; ADR-002 PostgreSQL isolation; ADR-003 Identity foundation.
Source: backend/src, backend/tests, scripts/NotifyHub.Database, scripts/Test-PostgreSql.ps1.
Frontend/src, routed UI, manifests/lock, tests/e2e, screenshots.
deploy migrations/grants; .github CI/templates; README/governance and developer docs.

## Latest Phase 2 increment
- API JWT middleware denies malformed/missing tokens, wrong signatures, expired/future
  tokens and wrong issuer/audience before querying session state.
- Valid signatures require current session state; revoked sessions and database failures
  return 401 without internal errors. The session probe response uses Cache-Control: no-store.
- No default signing key exists. Security:Jwt:PrivateKeyPath configures an operator-managed
  private PEM, minimum RSA 3072 bits; Issuer/Audience default to NotifyHub/NotifyHub.Web.
  Signing keys, key rotation/restore, browser login and owner MFA enforcement remain open.
- New HTTP cases use generated in-memory keys and a repository probe. They complement,
  rather than replace, the real PostgreSQL revocation/concurrency tests.
- Modern UI direction remains restrained violet/slate, accessible vector icons, consistent
  spacing, light/dark surfaces and responsive layouts; screenshot and axe checks passed.
## Repository publication preparation
Owner requested frontend, backend and root README be pushed to
https://github.com/RaoSaifUllah/NotifyHub.git. The remote has no advertised refs.
Independent Git repository initialized inside NotifyHub with main and origin.
README expanded with scope, actual implementation, setup, architecture, tests and
UI screenshots. Credentials, keys and generated outputs are ignored.
Staged-content scan found no configured database credentials/private keys/token
patterns, after reviewing and excluding the synthetic example.test parser fixture.
This limited scan is not a full security assessment. Push is pending the owner's
required AGPL-3.0-only confirmation. Implementation remains 15% done / 85% pending;
module percentages above are unchanged by publication preparation.
Owner explicitly renewed the source-push request and asked for an AGPL explanation.
Prepared publication without adding a LICENSE or treating the request as license adoption.

2026-10-01: git push -u origin main succeeded; main tracks origin/main.
Frontend, backend, README, tests and project documentation published as a development preview.
Local database credentials and generated artifacts remain excluded. No application deployment
or live notifications occurred. License adoption remains pending.
