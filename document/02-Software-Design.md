# Software design
Version 1.0 | 2026-10-01 | Architecture and security baseline

## Stakeholders and views
Users need predictable routing and understandable failures; operators need safe
self-hosting and recovery; contributors need module boundaries and tests;
security reviewers need threat controls and evidence. Views below cover runtime,
dependency, persistence, security, UX, deployment, and failure behavior.

## Technology decisions
Use .NET 10 LTS / ASP.NET Core and matching EF Core major with supported Npgsql,
Dapper read queries, PostgreSQL, React/TypeScript/Vite, Tailwind CSS, accessible
component primitives, TanStack Query and schema-based form validation.
Pin supported stable versions and lockfiles during setup; exclude prereleases.
Prefer Vite because this authenticated dashboard does not need SSR/SEO.
No Sites hosting requirement or cross-repository shared runtime dependencies.

## Runtime
Browser -> same-origin HTTPS reverse proxy -> static Frontend and /api/v1 API.
API -> PostgreSQL transaction (message, delivery snapshots, durable jobs).
Worker -> leased PostgreSQL jobs -> provider adapters -> external services.
Both hosts use the same infrastructure library and application handlers.
Deploy API and worker independently; neither uses in-memory-only queues.
PostgreSQL is the system of record; no Redis/RabbitMQ required in v1.

## Clean modular architecture
Planned solution:
- NotifyHub.Domain: entities, value objects, policy/state rules; no EF/HTTP dependencies.
- NotifyHub.Application: use cases, validation, authorization policies, repository,
  Unit of Work, clock, encryption, provider, and read-service interfaces.
- NotifyHub.Infrastructure: EF maps/migrations/repositories, Unit of Work,
  Dapper read services, job leases, crypto, SMTP/HTTP provider adapters.
- NotifyHub.Api: routing/controllers, middleware, auth schemes, DTO/OpenAPI mapping.
- NotifyHub.Worker: background execution/composition root, graceful shutdown.
Modules within layers: Identity, Workspaces, Applications, Destinations, Routing,
Messaging, Delivery, Operations. Keep namespaces and module APIs explicit.
Domain <- Application <- Infrastructure; Api/Worker compose Application and
Infrastructure. Domain never references an outer layer. Avoid microservices and
generic repositories exposing unrestricted IQueryable.

## Repository / Unit of Work / Dapper rules
Aggregate-specific repositories implement writes and aggregate loads via EF
Core. One scoped DbContext and IUnitOfWork per command. SaveChanges/Commit is
owned by the application use case; repositories never independently commit.
Message acceptance inserts message, immutable route snapshot, deliveries, and
jobs in a single transaction. No external HTTP/SMTP inside a DB transaction.
EF concurrency tokens protect conflicting edits; map conflicts to 409.
Dapper handles SELECT projections only through dedicated query interfaces.
Use a database credential with SELECT grants on authorized projection tables
(no provider ciphertext, password/token hashes, or key secret columns).
Grant only reviewed tables/views, not every future table. Every query explicitly
includes workspace/application scope; EF query filters do not protect Dapper.
All SQL parameterized; sort/filter identifiers come from allowlists.
Migrations run under a separate privileged deployment role. Security-critical
state transitions, job claims, and auth/key validation use the EF write context,
not a potentially stale read replica. V1 reads use primary PostgreSQL.

## Data model and invariants
All tenant-owned entities carry WorkspaceId; IDs use UUIDs; instants are UTC.
Composite tenant keys/FKs prevent references across workspaces. Index query
predicates including workspace and timestamp; use cursor pagination.
- User: normalized unique email, password hash/version, status, security stamp.
- Workspace/WorkspaceMember: unique membership, role, timezone, last-owner guard.
- Invite: workspace, email, hash, expiration, acceptedAt; single use.
- RefreshSession/RefreshToken: user, session/family, hash, parent/replacement,
  expires/used/revoked timestamps; never store raw refresh credentials.
- Application/ApiKey: workspace, application, prefix/hash, scopes, expiry/revocation.
- Destination: workspace, type, encrypted config, nonce/tag, key version, enabled.
- RoutingRule: ordered priority, typed conditions, destination links, version.
- Template: bounded plain-text placeholders; no executable expressions.
- Message: application/workspace, title/body, priority/topic/tags/JSON metadata,
  accepted time, status, idempotency key/fingerprint, route version snapshot.
- Delivery: message/destination, immutable safe routing snapshot, status,
  attempt count, next attempt, lease, terminal time, redacted outcome.
- DeliveryAttempt: timestamps, duration, outcome, safe provider reference.
- DurableJob: delivery, dueAt, leaseOwner/until, attempt fencing token.
- PushSubscription: workspace/user, endpoint and keys encrypted, opt-in.
- AuditEvent: actor/action/resource/workspace/time/correlation, redacted details.
Unique (ApplicationId, IdempotencyKey) for supplied keys; bound deduplication
window to configured retention (24h default). Same key plus same canonical
request returns original 202 result; different request returns 409.
JSON limits: title 200 chars, body 16 KiB UTF-8, metadata 8 KiB,
20 tags/64 chars each, total request 32 KiB (initial tunable safe defaults).

## Message and delivery lifecycle
Accept -> evaluate versioned routing -> persist jobs -> return 202.
No destinations: message Failed with reason NO_ROUTE, zero jobs.
Delivery states: Queued, Deferred, Processing, Succeeded, RetryScheduled,
DeadLetter, Cancelled. Public message states: Accepted, Queued, Processing,
Delivered, PartiallyDelivered, Failed. Before all deliveries terminal, message
remains Queued/Processing; all successes Delivered; some successes and some
terminal failures PartiallyDelivered; no successes and terminal failures Failed.
Worker claims due jobs atomically using EF transaction and PostgreSQL
FOR UPDATE SKIP LOCKED (targeted parameterized SQL via EF is allowed for claims).
Use expiring leases and fencing; heartbeat long work; reject stale completion.
Retry transient network errors, 429 and 5xx with jitter/exponential backoff,
bounded Retry-After, 8 total attempts and 24h age limit by default.
Permanent provider 4xx errors dead-letter unless adapter classifies explicitly.
Timeouts are uncertain outcomes; a crash after external acceptance can duplicate.
Reuse stable delivery identifiers as provider idempotency keys where supported.
Manual replay creates an audited new generation; never hide previous attempts.
Quiet hours store IANA timezone and local windows; defer to next allowed instant,
handle overnight windows and DST explicitly, with configurable critical bypass.
Rules use bounded operators (equals/contains/set membership), deterministic
ordering, union/deduplicate matching destinations; no arbitrary code or regex.
Snapshot routing/template data at acceptance; destination revocation still
blocks future attempts. Read current encrypted credentials at send time.

## Authentication and authorization
JWT access token: 5 minute lifetime, asymmetric signing, explicit issuer/audience,
algorithm allowlist, exp/nbf checks, small clock skew, kid-based rotation;
no secret/payload claims. Browser holds access JWT in memory only.
Opaque refresh token: at least 256 random bits; persist SHA-256 hash; browser
HttpOnly/Secure/SameSite=Strict cookie scoped to /api/v1/auth, no Domain.
Default session idle expiry 7 days, absolute expiry 30 days.
Refresh rotates atomically and invalidates old token; reuse revokes the family.
Frontend serializes refresh and coordinates tabs; concurrent reuse fails safely.
Lost refresh response requires login; no insecure reusable grace window.
CSRF token bound to session plus strict Origin validation on login/refresh/logout
and other cookie-auth operations. Exact CORS origins; no credentialed wildcards.
Every protected request validates active session/security stamp and current
workspace permission; logout/revoke/password reset disable the session at once.
If revocation store unavailable, fail closed; optimize only with bounded,
documented revocation semantics. Never trust role claims alone.
Use ASP.NET Core Identity lifecycle with reviewed Argon2id IPasswordHasher
implementation; benchmark parameters, salt and rehash policy. No custom crypto.
Reset/verification tokens are short-lived, single-use, hashed; generic responses.
Invite-only default; initial administrator created through one-time CLI/setup
with no default password. Require MFA for system admins/owners before production.
TOTP recovery codes hashed; rate-limited enrollment/verification and step-up
for credential changes, member roles, key issuance, and secret exports.
API key: random 256-bit secret, opaque identifier/prefix plus hash; show once.
Authorization scheme separate from JWT; default send-only app scope. Rotation
allows a short explicitly configured overlap, with immediate revoke option.

## Threat controls
- Tenant escape: mandatory policies, composite FKs and negative API/SQL tests.
- XSS: text rendering, bounded inputs, no raw HTML templates, CSP without unsafe-eval.
- Brute force: per-IP/account/key/workspace limits, cooldowns, nonenumerating responses.
- Secret disclosure: AES-256-GCM with unique random nonce per encryption, authenticated
  workspace/destination/key-version context; master keys outside DB/Git; versioned
  key rotation and tested restore. Redact headers, URLs/tokens, and provider errors.
- SSRF: HTTPS outbound destinations; block loopback/private/link-local/multicast,
  IPv4-mapped IPv6, cloud metadata and reserved ranges; validate all DNS answers.
  Pin validated connection addresses while preserving hostname/TLS checks; disable
  redirects and revalidate on every attempt. Restrict ports and custom headers.
  Egress firewall as defense in depth. Provider-specific allowlisted hosts.
  Self-hosted LAN SMTP/webhooks require an administrator-maintained explicit
  host/network allowlist; deny metadata targets regardless.
- Web Push endpoints: same SSRF controls, known push hosts/approved endpoints;
  remove expired subscriptions on 404/410. Browser consent required.
- SMTP: trusted configured host, TLS verification, credential protection;
  payload cannot choose SMTP host or arbitrary envelope recipients.
- Supply chain: locked dependencies, secret/SAST/dependency/container scanning,
  minimal non-root images, provenance and SBOM at release.
- Audit sensitive mutations without bodies/credentials; restricted immutable
  application behavior, retention and operator access documented.

## API contract
Base /api/v1. Auth: register, login, refresh, logout, forgot-password,
reset-password, verify-email, MFA setup/verify/recovery, sessions/list/revoke.
Workspace CRUD/invites/members; application CRUD; application key issue/rotate/revoke;
destination CRUD/test; rule CRUD/reorder; templates CRUD; push subscriptions;
message acceptance/list/detail; delivery attempts/replay; workspace settings;
system configuration/audit endpoints under restricted administration.
Use explicit workspace paths for dashboard resources. GET lists cursor/page
size <=100, filter allowlists. Provider test requires authorization/rate limit.
POST /messages uses API key; infer workspace/application from key, never trust
caller-supplied scope. Return 202 with id/status/Location after commit.
Errors use RFC 9457 Problem Details plus stable code and correlationId.
400 validation, 401 authentication, 403 forbidden, 404 scoped missing,
409 conflicts, 413 oversized, 429 rate limit with Retry-After, 503 unavailable.
Secrets only returned from issuance; all other DTOs explicitly redact.
Generate OpenAPI and a curl example; never embed real credentials.
Liveness /health; readiness /ready for database/host readiness; /metrics restricted.
Downstream provider outage affects delivery metrics, not API liveness.

## UX specification
Pages: login/recovery/MFA; overview; workspace switcher; applications/key issuance;
destinations with safe masked edit/test; ordered routing builder; templates and
quiet hours; searchable message history/details; dead-letter replay confirmation;
members/settings/sessions; system health and administration.
Modern restrained UI: consistent spacing/type, light/dark tokens, responsive
sidebar/table-to-card layouts, visible focus, accessible dialogs, empty/loading/
retry/validation states, reduced motion. Display timezone and delayed delivery
reason. Key copy is explicit once-only display; confirmation for revoke/delete.
History never caches across workspace switches or logout; clear query caches.
English first, translation files and logical CSS properties for Urdu/RTL.

## Deployment and operations
Compose services: reverse proxy/web, api, worker, postgres; optional local mail
mock for development. Internal DB network, TLS at trusted proxy, forwarded-header
allowlist, resource limits, persistent data and key storage outside image.
Single explicit migration job before rollout; API startup checks compatibility.
Use expand/contract migrations; take backup before destructive change.
Backup database, deployment configuration, and encryption/signing keys separately
with restricted access. Restore to isolated environment and verify decrypted
destinations plus pending-job recovery; never send test messages to live providers.
Metrics: acceptance, queue age/depth, delivery outcome/duration, retry/dead letters;
avoid tenant/message IDs as metric labels. Alerts on queue growth, job stalls,
DB unavailability and sustained provider failures. Logs use correlation IDs.
SIGTERM stops new claims and lets bounded in-flight work finish/release lease.

## Decisions and evolution
ADR-001 modular monolith with separate worker: fewer distributed failure modes.
ADR-002 EF write/repositories/UoW and Dapper projections: clear transaction ownership.
ADR-003 PostgreSQL durable queue: atomic acceptance without dual-write loss.
ADR-004 memory JWT + rotating cookie refresh: browser secret exposure reduced.
ADR-005 no native mobile in MVP: all required use cases covered by responsive web.
ADR-006 Web Push included: resolves source MVP inconsistency conservatively.
Scale worker replicas first; only introduce broker/partitioning after measured need.
