# ADR-005: atomic credential login and single-use MFA challenges

Date: 2026-10-02. Status: accepted increment; Phase 2 remains incomplete.

## Context
Identity persistence and cookie sessions existed. Administrators/owners needed
mandatory enrollment and a browser login flow without issuing a session after
only a password. Reusing a TOTP within its accepted window must be rejected.

## Decision
Keep password and MFA credential commands in the infrastructure Identity module,
behind the application ICredentialLogin contract and one Unit of Work.
Serialize credential mutations on the account row. Password failures, MFA
failures and lockout changes commit together; successful password verification
does not reset failed MFA counts. Only complete authentication resets counts.

Issue random, SHA-256-hashed five-minute challenges bound to a CSRF hash and
current security stamp. Enrollment replaces an unconfirmed authenticator key,
invalidating prior enrollment proofs. Consume the proof in the same transaction
that verifies MFA and creates the session. Recovery invalidates earlier sessions
and outstanding proofs through a security-stamp change.

Use pinned Otp.NET 1.4.1, already exercised for interoperability, to validate TOTP
with current/adjacent time steps. Persist the matched step on the locked account
and reject equal/older steps. No custom TOTP implementation is introduced.
Generate ten 256-bit recovery codes, store hashes and show plaintext only once.
Store an authoritative MFA verification timestamp in the session, not role claims.

The EF Identity store disables AutoSaveChanges and overrides UpdateAsync to keep
the original DB concurrency stamp through compound commands. The framework base
implementation re-attaches on every update, accepting an uncommitted stamp as
an original when multiple updates precede the command-owned commit. A real
PostgreSQL enrollment test exposed this and guards the corrected behavior.

Browser access tokens and authentication proofs remain in memory. Only a
host-bound Secure/SameSite Strict CSRF cookie is JS-readable; refresh stays
HttpOnly. Refresh/logout use Web Locks to serialize tabs. BroadcastChannel sends
only session-change/revocation signals, never tokens. Unsupported coordination
fails closed. The UI keeps enrollment/recovery secrets out of browser storage.

## Evidence and remaining work
Real PostgreSQL: enrollment, replay, recovery single use, lockout and session
invalidation; concurrent proof consumption and read-role restrictions.
HTTP fixtures: Origin/CSRF, generic errors, one-time session creation contract.
Playwright with mocked auth responses: enrollment/recovery UI, secret-storage
checks, responsive themes and automated accessibility.

These layers are complementary; they are not a full production browser-to-database
authentication deployment test. Fresh step-up endpoints/policies, invitations,
verification/reset challenges, audit, key rotation/restore and complete account
management remain Phase 2 work.

Sources:
- https://github.com/kspearrin/Otp.NET (matched-step persistence responsibility)
- https://www.nuget.org/packages/Otp.NET/1.4.1
- https://github.com/dotnet/aspnetcore/blob/v10.0.12/src/Identity/EntityFrameworkCore/src/UserStore.cs