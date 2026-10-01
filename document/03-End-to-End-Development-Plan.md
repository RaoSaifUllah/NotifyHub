# End-to-end development plan
Version 1.0 | 2026-10-01
Execute in dependency order. Each milestone must build and pass its exit checks
before subsequent features depend on it. No calendar estimate is assumed.

## Phase 0: baseline and decisions
Read original SRS plus refined requirements/design. Record any justified changes
as ADRs. Confirm available SDK/Node/Docker versions; pin supported stable packages.
Create solution and frontend manifests/lockfiles, editorconfig, ignore rules,
development secrets examples, README quickstart and contributor setup.
Select license with owner before publication, prepare SECURITY/CONTRIBUTING/
CODE_OF_CONDUCT/CHANGELOG and issue/PR templates without fabricated contacts.
Exit: clean checkout setup reproducible; no secrets; all skeleton builds pass.

## Phase 1: architecture and persistence
Create five .NET projects with dependency boundaries and module namespaces.
Implement PostgreSQL EF migrations, aggregate repositories and one Unit of Work,
Dapper scoped SELECT services and distinct read/write/migration credentials.
Add error middleware, validation, UTC clock, correlation IDs, structured logging,
health/readiness, OpenAPI, DI, frontend routing/design tokens, test fixtures.
Exit: migrations work from empty DB; transaction rollback verified; architecture
dependency checks pass; Dapper role cannot write or read secret tables.
Maps to NH-13/14 foundation.

## Phase 2: identity and workspaces
Implement bootstrap admin, configurable registration/invites, password hashing,
email verification/reset with mock SMTP, JWT validation, rotating refresh sessions,
CSRF, session revoke, MFA and recovery, workspace/member policies and audit.
Build login, recovery, MFA, sessions, workspace switcher/member screens.
Exit: refresh replay/concurrency tests, logout/password-reset invalidation,
last-owner protection, role and cross-tenant tests all pass; no browser token
persistence; invite-only installation defaults secure.
Maps to NH-01/02.

## Phase 3: applications, keys and durable ingestion
Implement application CRUD and scoped API key issue/rotate/revoke UI/API.
Validate notification envelope, enforce request/rate limits and idempotency.
Persist message/route snapshot/deliveries/jobs atomically. Implement queue claim,
leases, fencing, retries, dead letter and aggregate statuses using fake adapter.
Exit: rollback leaves no orphan jobs; crash/restart processes accepted messages;
same-key concurrency creates one message; changed payload conflict returns 409;
revoked and cross-application keys fail; publish documented curl example.
Maps to NH-03/04/07/14.

## Phase 4: routing and provider delivery
Implement typed rules, ordering/deduplication, no-route failure, safe templates,
timezone quiet hours and critical bypass. Implement encrypted destination CRUD,
key rotation, SSRF-safe transports and provider test.
Adapters in order: generic webhook, SMTP Email, Telegram, Discord, Web Push.
Use provider mocks/contracts first; optional live smoke tests only with user
credentials and explicit authorization to send test notifications.
Build destination/rule/template/quiet-hours screens and push opt-in UI.
Exit: every adapter covers success, permanent error, transient error and timeout;
429 respects bounded Retry-After; secrets never exposed; SSRF/rebinding/redirect
tests pass; DST and overnight quiet-hours tests pass; push consent and expiry work.
Maps to NH-05/06/09.

## Phase 5: complete dashboard
Build overview, applications, keys, history/search/details, failed-delivery replay,
workspace settings and restricted system-health/admin pages.
Add query cancellation/cache reset on scope changes; pagination/filtering,
masked secrets, one-time key view, accessible responsive interactions and themes.
Exit: Playwright covers full workflow and read-only restrictions; keyboard and
screen reader review; mobile viewport and current browser checks; WCAG 2.2 AA
checklist plus reported performance budgets.
Maps to NH-08/10.

## Phase 6: operational hardening
Implement retention/cleanup, audit access, backup/restore scripts and runbook,
metrics/alerts, shutdown behavior and deployment limits.
Create production multistage Dockerfiles, Compose, proxy/TLS examples and
development fixtures. Run migrations as explicit deployment step.
CI: format/lint/typecheck -> unit/integration/E2E -> build -> secret/SAST/
dependency/container scans -> image/SBOM artifacts. Use PostgreSQL integration
tests with actual constraints; SQLite is not a substitute.
Exit: fresh Compose install, upgrade and isolated backup restore proven;
reference load measured; no unresolved critical/high vulnerabilities without
documented accountable exception; no actual certification claim.
Maps to NH-11/12/13/14.

## Phase 7: release handoff
Complete installation, setup, API examples, provider guides, deployment, restore,
upgrade, troubleshooting, limitations and contribution documentation.
Resolve license selection. Produce versioned changelog, release notes, image
references, checksums/SBOM and migration notes; document all verification results.
Obtain repository URL before configuring a remote or pushing; initialize Git
only inside NotifyHub if requested. Publishing/deploying remains a user action
unless explicitly authorized.
Exit: requirements matrix has linked evidence for every NH requirement; clean
checkout builds and tests; owner can install and restore using documentation.

## Test pyramid and regression priorities
Unit: role/rule logic, template bounds, quiet-hours/DST, status aggregation,
retry classifications. Integration: PostgreSQL constraints, atomic acceptance,
refresh races, leases/fencing, retention, encryption rotation, Dapper scopes.
Contract: fake provider servers and versioned request/response fixtures.
E2E: owner setup -> destination -> application/key -> route -> submit ->
delivered history; transient failure -> retry; permanent failure -> replay;
read-only and cross-workspace denial; session revoke; Web Push consent.
Fault tests: kill worker before/after external acceptance; DB outage during
acceptance; lease expiry; duplicate job claims; malformed provider responses.
Measure behavior and document duplicate-delivery limitation.

## Tracking and handoff
Maintain document/Implementation-Status.md during development: milestone,
changed files, commands/results, unresolved risks and next action. Mark
Not started / In progress / Verified; never label missing checks as passing.
Avoid cross-project dependencies. Stop adding features when v1 gates pass.
