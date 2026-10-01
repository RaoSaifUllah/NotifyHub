# Requirements and source review
Version 1.0 | 2026-10-01 | Status: implementation baseline proposal

## Portfolio review
| Project | Main complexity | Backend/mobile implications | Preparation order |
|---|---|---|---|
| NotifyHub | durable delivery, provider credentials, SSRF | .NET API/worker; responsive web sufficient | 1 |
| TaskFlow | recurrence/timezones and collaboration | .NET API; native mobile useful later | 2 |
| DocuNest | hostile files, OCR, storage, permissions | .NET API/worker; native camera/share app useful later | 3 |
| LocalTools | WASM, browser memory, offline privacy | preserve zero mandatory backend; backend folder can remain optional | 4 |
| DropBridge | WebRTC, pairing, TURN, browser compatibility | .NET signaling; native mobile is a separate capability decision | 5 |

This plan follows source section 118. Only NotifyHub is scaffolded now.
The requested .NET stack supersedes Go/Node/Python suggestions for server
application logic. Do not force login/database infrastructure into LocalTools'
local processing path. Do not infer that every project requires native mobile.

## Source gaps and decisions
- Section 21 includes Web Push in MVP; sections 41 and 126 omit it. Include Web
  Push in v1 to satisfy the broader requirement, with a documented HTTPS and
  supported-browser prerequisite.
- Quiet hours and templates are included in v1; third-party inbound adapters,
  Slack/Teams, OIDC/LDAP, enterprise SSO, billing, and native apps are later.
- No matched routing rule must produce an explicit unrouted result, never a
  silent successful delivery.
- Delivered means accepted by the destination API/SMTP server, not read by a user.
- Exactly-once external delivery is not guaranteed. Expose possible duplicates.
- Retention defaults to 30 days; forever requires explicit administrator choice.
- Provider payloads stay out of operational logs by default; message history is
  privileged retained application data.
- Compliance is a verification target, not a certification claim.

## Actors and permission policy
System administrator manages instance configuration, registration, retention,
health, and users; does not automatically read every workspace's message data.
Workspace owner manages membership and all workspace resources. Developer
manages applications, scoped keys, destinations, rules, messages, and retries,
but cannot change owners or instance settings. Read-only user reads authorized
history/configuration with credential fields always redacted, and cannot mutate.
API keys belong to exactly one application and workspace; default scope is
messages:send. Optional messages:read is application-scoped. Keys cannot access
dashboard administration. Every route, SQL query, and worker operation enforces
workspace boundaries. Last owner removal is prohibited.

## Functional baseline and traceability
| ID | Requirement / measurable acceptance | Source | Phase |
|---|---|---|---|
| NH-01 | Register under configured invite/open/disabled mode; login, reset, logout, sessions, revocation work | Common 91; auth request | 2 |
| NH-02 | Create workspace and membership with server-enforced role policy | 20,36 | 2 |
| NH-03 | Create application; issue key once; rotate/revoke and scope it | 22,23 | 3 |
| NH-04 | Accept valid notification durably with 202 and status URL; invalid input 400; key abuse 429 | 19,28,37 | 3 |
| NH-05 | Deterministic application/topic/priority/title/tag routing to distinct destinations | 24-26 | 4 |
| NH-06 | Configure/test Email, Telegram, Discord, webhook, Web Push without exposing stored secrets | 21,34,39 | 4 |
| NH-07 | Durable jobs survive restart; bounded retries, dead letters, authorized replay | 29,30 | 3-4 |
| NH-08 | History shows delivery states, attempts, redacted errors, filters and pagination | 30-32,38 | 5 |
| NH-09 | Timezone-based quiet hours, optional critical bypass and safe variable templates | 27,33 | 4 |
| NH-10 | Responsive accessible themed dashboard with all section 38 pages | 38,88,113 | 5 |
| NH-11 | Retention purges expired payloads; secrets encrypted; auditable sensitive actions | 32,39,91 | 6 |
| NH-12 | Compose deploys web/API/worker/PostgreSQL; backup and restore verified | 41,102,106 | 6 |
| NH-13 | Health/readiness, JSON logs, tracing and bounded-cardinality metrics | 40,95-97 | 6 |
| NH-14 | All published endpoints documented in versioned OpenAPI with examples and errors | 98,99 | 3-6 |

## Nonfunctional acceptance
- Reference load: Linux 4 vCPU/8 GiB, PostgreSQL on SSD, 20 users, 100,000 retained
  messages, 20 accepted messages/second for 15 minutes. Record actual hardware.
- CRUD p95 <500 ms and message acceptance p95 <500 ms on that profile; remote
  provider latency excluded. Search/history p95 <1 second.
- Dashboard usable within 2 seconds on a recorded broadband test profile;
  Lighthouse performance >=85, accessibility >=90; manual WCAG checks still required.
- In healthy providers, p95 queue-to-first-attempt <5 seconds at reference load.
- Recover accepted messages after API/worker crash; atomicity and lease tests prove it.
- Proposed operational targets: backup RPO 24h, restore RTO 2h; prove by restore drill.
- UTC instants, IANA timezones, UTF-8, English initial strings externalized and RTL-ready.
- No mandatory telemetry or cloud dependency for core routing except chosen providers.

## Open decisions
Owner must supply the Git remote before any push and choose a license before
publication. Provider credentials, public hostname, production secret store,
SMTP, and Web Push VAPID keys are deployment inputs; use local mocks during
development. Do not block implementation on these inputs.
