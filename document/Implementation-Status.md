# Implementation status
Updated: 2026-10-02 (Asia/Karachi). NotifyHub v1 is incomplete.
Percentages are estimates of implementation scope, not coverage, certification or assurance.

## Milestone progress
| Phase | State | Done | Pending |
|---|---|---:|---:|
| 0 Baseline and decisions | Verified local skeleton exit checks | 100% | 0% |
| 1 Architecture and persistence | Verified foundation exit checks | 100% | 0% |
| 2 Identity and workspaces | In progress; login/MFA and browser flow tested | 62% | 38% |
| 3 Applications, keys and ingestion | Not started | 0% | 100% |
| 4 Routing and providers | Not started | 0% | 100% |
| 5 Complete dashboard | Feature work not started; foundation UI exists | 0% | 100% |
| 6 Operational hardening | Not started; baseline observability/CI exists | 0% | 100% |
| 7 Release handoff | Not started | 0% | 100% |

Overall v1 estimate: **22% implemented, 78% pending**. Phases have unequal size.

## Module progress
| Module | Done | Pending | Implemented / remaining |
|---|---:|---:|---|
| Tooling, solution, dependency locks | 100% | 0% | Five production layers, DB helper, tests, SDK and package locks |
| Architecture / persistence foundation | 100% | 0% | EF repositories/UoW, scoped Dapper view, migrations, actual PostgreSQL checks |
| Accounts / JWT / refresh / MFA / RBAC | 70% | 30% | Identity/Argon2id, JWT issuer, rotating session services, protected MFA store tested; Bootstrap, password login, mandatory owner/admin MFA, recovery and cookie transport tested; reset/invites/step-up/audit pending |
| Workspaces / membership | 25% | 75% | Persistence, permission rules, locking and last-owner use case; invitations/APIs/step-up pending |
| Applications / scoped keys | 0% | 100% | Not implemented |
| Ingestion / idempotency / queue / retries | 0% | 100% | Not implemented; worker is a composition root only |
| Routing / templates / quiet hours | 0% | 100% | Not implemented |
| Email / Telegram / Discord / webhook / Web Push | 0% | 100% | Not implemented; no notifications sent |
| Dashboard / history / replay | 20% | 80% | Modern overview and login/MFA/recovery UI; workspace, history and notification data pending |
| Provider encryption / SSRF / audit / retention | 0% | 100% | Not implemented; identity secret protection is counted above |
| Observability / deployment / restore | 10% | 90% | JSON logs, health/readiness, redacted errors; Compose/metrics/runbooks/full restore pending |
| Verification / CI / documentation | 25% | 75% | Local suites and authored PG CI; complete journeys, scans, load/release evidence pending |

## Actual implemented behavior
- Domain/Application dependency boundaries; independent API/worker composition roots.
- Scoped aggregate repositories and one command-owned Unit of Work.
- PostgreSQL migrations: InitialWorkspace, IdentityFoundation, RotatingSessions, CredentialLogin.
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
  Public password login, mandatory administrator/owner enrollment, MFA/recovery and secure cookie endpoints are implemented.
- TOTP secrets encrypted through ASP.NET Data Protection with account-bound purpose.
  High-entropy recovery code hashes redeem atomically while locking the account.
  Enrollment, login and recovery HTTP flows/UI are implemented; fresh step-up and management remain pending.
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
1. Complete Phase 2: registration/invites, email verify/reset mocks and single-use
   challenges, fresh MFA step-up/management, immutable audit, workspace/member/session
   APIs and their frontend journeys. Login, enrollment, recovery and lockout now exist.
2. Verify account/session APIs and cross-workspace policies before Phase 3.
3. Then proceed Phase 3 through Phase 7 in documented order.
4. Development Data Protection keys are in ignored output/secrets. Production needs
   a stable restricted key directory, encryption/protection policy and full restore drill.
   Local key-ring reopening is not the required production backup/restore verification.
5. Provider contracts, full notification Playwright journey, browser/screen-reader review,
   SAST/secret/container scans, SBOM, reference load, Compose install/upgrade and restore
   remain unexecuted. ASVS control-by-control evidence is not complete.
6. Docker is needed for later Compose gates. GitHub Actions is disabled by owner
   instruction; the archived workflow is reference only. Perform later scanning,
   release and verification locally unless the owner changes that policy.
7. Owner supplied the GitHub repository and confirmed AGPL-3.0-only on 2026-10-01.
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

## AGPL-3.0-only adoption — 2026-10-01
Owner explicitly requested applying and pushing AGPL v3 after receiving the explanation.
NotifyHub adopts AGPL-3.0-only, without the "or later" option.
LICENSE contains the official GNU AGPL v3 text downloaded from gnu.org and checked
for the title, version and network-interaction section. README and contributor
guidance now identify the adopted license; earlier pending-license notes above
describe the preceding history and are superseded by this adoption.
No runtime behavior or dependency versions changed. Full application test suites
were not repeated for this license/documentation-only increment.
Implementation remains 15% done / 85% pending, with module percentages unchanged.
## Publishing workflow change — 2026-10-01
Owner instructed future changes to merge into main locally and push main directly,
without GitHub Actions. Existing CI YAML moved to .github/disabled-workflows/ci.yml
so it is retained as reference but no longer registered as an active workflow.
Local build, unit, PostgreSQL, provider-contract and browser checks remain required
as appropriate. Future hosted CI work in the development plan is superseded by this
owner instruction until explicitly changed. No active YAML workflows remain.
Implementation percentages are unchanged: 15% done / 85% pending.
## Phase 2 increment — 2026-10-02
Implemented:
- One-time AccountBootstrap with transaction-owned persistence and PostgreSQL advisory
  lock; creates only the initial administrator, rejects existing-account databases.
- Interactive NotifyHub.Admin CLI reads/confirm passwords without echo, rejects
  redirected input and password arguments, and suppresses sensitive exception details.
- HTTPS-only POST /api/v1/auth/refresh and /logout with explicit exact Origin allowlist,
  session-bound CSRF, rotating HttpOnly/Secure/SameSite Strict cookies scoped to auth,
  no-store responses and queue-free auth request limits.
- CLI added to solution with locked dependencies; no production administrator created.
- Identity-Development.md documents setup, configuration, actual endpoints and limitations.
  ADR-004 records the transport/bootstrap choices.

Actual commands/results:
| Command | Result |
|---|---|
| dotnet restore NotifyHub.slnx --locked-mode --verbosity quiet | PASS, including new Admin project lockfile |
| dotnet build NotifyHub.slnx --no-restore --verbosity quiet | PASS, zero warnings/errors |
| dotnet test backend/tests/NotifyHub.UnitTests --no-build --verbosity quiet | PASS, 20/20 |
| dotnet test backend/tests/NotifyHub.IntegrationTests --filter FullyQualifiedName~BrowserSessionTests --verbosity quiet | PASS, 13/13 |
| ./scripts/Test-PostgreSql.ps1 | PASS, 31/31 total integration cases, including actual PostgreSQL |
| dotnet run --project scripts/NotifyHub.Admin --no-build | PASS, usage only; no account mutation |

New PostgreSQL evidence: two simultaneous bootstrap attempts produce exactly one
administrator, validation leaves no account, repeat setup refuses replacement,
Argon2id/confirmed-email/lockout flags persist, wrong-CSRF cookie logout leaves
session active, correct-CSRF logout revokes it. Existing session/MFA/workspace and
HTTP regression checks also passed. HTTP transport tests verify Origin/HTTPS denial,
cookie attributes, no refresh secret in JSON, replay revocation, logout and 429 limits.
UI source and screenshots unchanged; browser tests were not repeated for this
backend-only increment. Provider tests remain not applicable until adapters exist.

Next Phase 2 work: verified credential login and lockout, single-use authentication
challenges, registration/invites/reset/verification with local email mocks, MFA
enrollment/recovery/login/step-up, audit and workspace/member/session APIs and
modern frontend flows. Do not skip Phase 2 exit checks to start ingestion.
Bootstrap alone does not provide a login or bypass mandatory administrator MFA.
Production key protection and distributed rate limits remain unverified.
Current estimates: overall 17% done / 83% pending; Phase 2 48% done / 52% pending.
## Credential login and MFA increment — 2026-10-02
Implemented:
- Generic password login with confirmed/enabled eligibility, account lockout, dummy
  password-hash verification for unknown/ineligible accounts and bounded hashing requests.
- Mandatory administrator/owner MFA; password-only response never grants privileged session.
- Five-minute, hashed, CSRF/security-stamp-bound enrollment/verification challenges.
- Account-locked TOTP time-step replay protection with pinned Otp.NET 1.4.1; ten
  high-entropy hashed recovery codes and security-stamp invalidation after recovery.
- Authoritative MFA verification time in sessions, preserved through refresh.
- HTTP login/MFA contracts and host-bound Secure CSRF cookie; refresh remains HttpOnly.
- Responsive light/dark /login, enrollment, verification, recovery and one-time code
  view; memory-only JWT/proofs, Web Locks refresh/logout coordination, token-free
  BroadcastChannel notifications, sign-out and generic errors.
- Owner-restricted local RSA setup CLI and ignored configuration; no operator account
  was created. CredentialLogin migration and reviewed grants applied to the dedicated DB.
- README remains description/how-to-use/UI previews; added sign-in screenshot.
  ADR-005 and Identity-Development.md record choices and limitations.

Verified commands/results so far:
| Command | Result |
|---|---|
| dotnet test backend/tests/NotifyHub.UnitTests --verbosity quiet | PASS, 22/22 |
| dotnet test backend/tests/NotifyHub.IntegrationTests --filter CredentialLoginEndpointTests/BrowserSessionTests | PASS, 16/16 |
| ./scripts/Test-PostgreSql.ps1 | PASS, 34/34 final repeat including concurrent MFA, role restrictions and account eligibility |
| npm run build --prefix Frontend | PASS |
| npm run test:e2e --prefix Frontend | PASS, 9/9; login/overview axe checks in both themes report zero violations |
| dotnet run --project scripts/NotifyHub.Database --no-build -- migrate | PASS, fourth migration applied |
| dotnet run --project scripts/NotifyHub.Database --no-build -- grants | PASS, explicit challenge write grant |
| dotnet run --project scripts/NotifyHub.Admin --no-build -- init-signing-key | PASS, RSA 3072 and restricted Windows ACL; ignored local settings |
| git check-ignore secrets/jwt/private.pem backend/src/NotifyHub.Api/appsettings.Local.json | PASS |

PostgreSQL evidence: enrollment supersedes old proof, CSRF denial, consumed/hashed
challenges, encrypted authenticator storage, one-time TOTP, recovery-code reuse
denial, earlier-session invalidation, failures across newly issued challenges still
reach lockout, concurrent completion yields one session, read role cannot query
user/token/session/challenge tables. Final repeat adds ordinary confirmed login and
unconfirmed/disabled denial. Password/MFA requests remain local only.

Resolved failures:
- Initial enrollment exposed the framework store's repeated Attach resetting original
  concurrency values before a Unit of Work commit. Custom UpdateAsync preserves the
  original snapshot; full PostgreSQL regression passed after the fix.
- One HTTP test build overlapped a running testhost and hit Windows DLL locks. Builds
  and backend test runs were serialized afterwards.
- Signing-key setup first failed platform analysis for an unconditional Unix setter,
  then an unnecessary Windows ownership change. Platform guards and ACL-only updates
  corrected both; setup succeeded with inheritance disabled and one owner access rule.

Pending: registration/invites, single-use email verification/reset with local mail mocks,
fresh MFA step-up and management, immutable audit, session/workspace/member APIs and
their UI. Local HTTPS operator setup, key rotation/restore, production Data Protection
protection and a full browser-to-PostgreSQL deployment journey remain unverified.
Playwright auth uses mocked HTTP responses; actual credential use cases are tested
separately against PostgreSQL. No certification/full-ASVS claim is made.
Estimated overall 22% done / 78% pending; Phase 2 62% done / 38% pending.
Final eligibility repeat: ./scripts/Test-PostgreSql.ps1 passed all 34/34 cases
after adding confirmed ordinary-user authentication and unconfirmed/disabled
rejection. The isolated PostgreSQL case completed in 2 minutes 27 seconds.
All failures recorded above were corrected before publication; no pending test
failure is being hidden. Overall and module percentages remain estimates.
Final locked restore and solution build passed with zero warnings/errors.
EF reported no model changes since CredentialLogin. NuGet's transitive advisory
check reported no known vulnerable packages across all nine projects at this run.
Repeated signing setup correctly refused existing configuration and left the
private key/local settings unchanged. These checks do not replace production
key-rotation, restore or independent security assessment.
### Graceful stop — 2026-10-02

Stopped feature work at the user's request and prepared the verified increment for
local merge and direct push to main. Final Playwright run passed 9/9 over local
HTTPS. Authentication refuses non-HTTPS submissions before transmitting passwords.
The test command generates a one-day localhost TLS fixture under ignored TestResults;
it does not install a trusted certificate or send live notifications.

Resume with the remaining Phase 2 identity and workspace work listed above.
Overall estimate remains 22% complete / 78% pending; Phase 2 is 62% / 38%.
