# Development chat prompt
Copy the following prompt into a new development chat with this workspace:

Implement NotifyHub in F:RF TechWorkingOpenSourceProjectsNotifyHub.

Read README.md and document/01-Requirements-and-Review.md through
document/04-Verification-and-Standards.md first. Use
document/00-Original-Portfolio-SRS.md as source context; the refined NotifyHub
requirements and my requested .NET stack take precedence over its alternative
technology suggestions.

Build the complete self-hosted NotifyHub v1 following the end-to-end plan:
.NET 10 LTS latest supported stable patch, ASP.NET Core API and separate worker,
clean modular architecture, aggregate repositories and Unit of Work with EF Core
for writes, Dapper for tenant-scoped SELECT projections, PostgreSQL durable queue,
JWT access authentication with secure rotating refresh tokens, and all documented
security controls. React/TypeScript/Vite frontend with Tailwind CSS, modern
responsive light/dark UI, accessible components and translation-ready strings.
No native mobile app is required for this project.

Implement secure accounts/MFA/workspaces/RBAC, applications/scoped API keys,
durable notification acceptance/idempotency, deterministic routing, quiet hours,
safe templates, Email/Telegram/Discord/webhook/Web Push adapters, retries/dead
letters, message history/dashboard, encryption/audit/retention, observability,
Docker Compose deployment, migrations, backup/restore, documentation and CI.
Follow OWASP ASVS Level 2 targets and the documented standards traceability;
do not claim certification or guaranteed security without verified evidence.

Work milestone by milestone with meaningful unit, real PostgreSQL integration,
provider-contract and Playwright tests. Use local mocks instead of requesting
real provider secrets early. Never send live notifications without my explicit
authorization. Maintain document/Implementation-Status.md with actual verification
results, unresolved risks and next steps. Recheck stable package compatibility
and pin versions/lockfiles. Record design changes as ADRs.

Only work inside NotifyHub; keep it an independent future Git repository and
do not modify or scaffold the other four portfolio projects. No remote is
configured yet. Ask for the repository URL when a remote/push is needed; do not
guess it or push/publish/deploy externally without authorization. License is
proposed AGPL-3.0-only and requires my selection before publication.
Start by inspecting available tools and the scaffold, then execute Phase 0
and continue through the plan. Report completed behavior and actual checks,
not merely intended work.
