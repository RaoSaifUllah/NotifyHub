# ADR-004: browser session transport and one-time local bootstrap

Date: 2026-10-02. Status: accepted implementation increment, Phase 2 incomplete.

## Context
The design requires memory-only access tokens, rotating cookie refresh, strict
Origin/CSRF boundaries and administrator setup without default credentials.
Identity/session primitives existed but had no public cookie transport or CLI.

## Decision
Keep bootstrap outside HTTP in an interactive operator CLI with no password
arguments or echo. Serialize it with a transaction-scoped PostgreSQL advisory
lock and close it once any account exists. Email confirmation is an explicit
local operator trust decision. Creation does not bypass administrator MFA.

Cookie endpoints require HTTPS and an explicitly configured exact Origin.
Refresh secrets use an HttpOnly/Secure/SameSite Strict cookie scoped to auth.
Use a browser-session cookie instead of a persistent expiry; server idle and
absolute limits still bound access. Return only access/CSRF/session data in JSON.
Logout requires the same session-bound CSRF and commits revocation before removal.
Use no-store responses and a bounded, queue-free per-IP auth transport limiter.

## Consequences and limits
Missing configuration denies authentication; the HTTP-only preview remains usable
for health but not browser cookies. Public login and MFA enrollment are still
needed to initiate a browser session. Single-process rate limits are not a
multi-instance protection mechanism. No loose Origin matching, insecure cookie
mode or refresh replay grace window is introduced.

Verification uses HTTPS HTTP fixtures plus actual PostgreSQL bootstrap/session
locks. Key rotation, MFA strength/step-up, audit and complete browser auth journeys
remain required before Phase 2 exits.