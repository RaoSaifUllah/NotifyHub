# Identity development

Updated 2026-10-02. Phase 2 is incomplete. This guide describes implemented
setup/session behavior; it is not an installation or production-security sign-off.

## One-time administrator setup

Apply migrations and restricted grants following Database-Development.md first.
From the NotifyHub root, open an interactive terminal and run:

    dotnet run --project scripts/NotifyHub.Admin -- bootstrap

Enter the email and a unique password of 12–256 characters, then confirm it.
The password is read without echo, never accepted as a command argument, and
stored only as an Argon2id hash. Redirected input is refused.

The CLI uses the configured Write connection, not the schema-owner credential.
A transaction-scoped PostgreSQL advisory lock serializes all setup attempts.
If any account exists, setup refuses to create or replace an administrator.
Failed validation and failed creation roll back. There is no default password
or HTTP bootstrap endpoint.

The operator-supplied initial email is marked confirmed. The account starts with
lockout enabled and system-administrator status, but without enrolled MFA.
Password login, enrollment and recovery are now implemented; fresh step-up and
account management are pending. Setup alone does not bypass required MFA. Do not treat this as a production-ready
authentication installation.

## Local signing key setup

From the NotifyHub root:

    dotnet run --project scripts/NotifyHub.Admin -- init-signing-key

Creates a 3072-bit RSA private PEM under ignored secrets/jwt and atomically configures
Security:Jwt in ignored local settings. Existing keys/configuration are not replaced.
Windows ACL inheritance is disabled with access only for the operator identity;
Unix creation uses owner-only permissions. Use the same authorized identity for
the local API, or explicitly configure the production service account separately.
The operator must arrange a restricted backup. This command is initial setup,
not key rotation or a verified production secret-management workflow.

## Signing and browser transport

An operator-managed RSA private PEM of at least 3072 bits is configured by
Security:Jwt:PrivateKeyPath. Keep keys outside Git in a restricted directory.
Issuer/audience default to NotifyHub/NotifyHub.Web. Missing signing configuration
does not permit authentication. Rotation and production key backup are pending.

Set Security:BrowserOrigins to an explicit array of exact browser origins, e.g.:

    {
      "Security": {
        "BrowserOrigins": ["https://notifyhub.example"]
      }
    }

Put deployment/local values in ignored settings or environment variables.
There are no default trusted origins. Origin null, omitted Origin, near-match
subdomains and trailing-slash variants are rejected. Cookie session endpoints
require HTTPS even in Development. Do not disable Secure for local tests;
integration fixtures simulate HTTPS. The current HTTP UI preview can still
show health, but browser authentication requires a local HTTPS setup.

## Implemented session endpoints

POST /api/v1/auth/refresh reads the __Secure-notifyhub-refresh cookie and one
X-CSRF-Token header. Exact Origin, HTTPS, bounded inputs and session-bound CSRF
are enforced. On success it atomically rotates the token and returns accessToken,
csrfToken, sessionId and expiresIn (300 seconds). The refresh secret is only in
Set-Cookie, never JSON. A separate __Host-notifyhub-csrf cookie is JS-readable
and bound server-side to the session; it contains no JWT or refresh token.

Cookie attributes: HttpOnly, Secure, SameSite=Strict, Path=/api/v1/auth, no Domain.
It is a browser-session cookie; server-side seven-day idle/thirty-day absolute
limits remain authoritative. A refresh response lost in transit requires login;
there is no reusable grace period. Replay revokes the session family.

POST /api/v1/auth/logout uses the same Origin/HTTPS/cookie/CSRF boundary.
It revokes the session before deleting the cookie. A wrong CSRF token does not
revoke the database session. Revoked access tokens fail their next active-session
check. Both responses use Cache-Control: no-store.

The auth transport has an in-process, per-client-IP fixed-window limit of
10 requests/minute, with no queue. Multi-instance/global limits and trusted-proxy
configuration remain operational work. Do not trust arbitrary forwarded headers.

GET /api/v1/auth/session validates the bearer token and current session; it
does not replace login. Login and enrollment are available from /login when HTTPS, exact Origin and
signing/database setup are configured. Invites, registration and verification/reset
challenges remain pending.

## Verification

    dotnet build NotifyHub.slnx --no-restore --verbosity quiet
    dotnet test backend/tests/NotifyHub.IntegrationTests --filter FullyQualifiedName~BrowserSessionTests
    ./scripts/Test-PostgreSql.ps1

The HTTP fixture verifies transport/cookie contracts against real SessionService
logic with a controlled repository. The PostgreSQL fixture separately verifies
actual locks, bootstrap concurrency, rollback, refresh races and cookie-logout
revocation. No fixture bootstraps a real operator account or sends notifications.
## Credential login and MFA

POST /api/v1/auth/login accepts email/password behind the HTTPS/Origin boundary.
Unknown, disabled, unconfirmed, locked and incorrect credentials have the same
generic failure response. Password hashing work is bounded by a two-slot,
queue-free HTTP gate in addition to per-IP rate limits and account lockout.

Administrators and owners cannot receive a session without MFA. A correct
password returns either EnrollMfa or VerifyMfa with a random five-minute challenge
and CSRF value. Enrollment also returns the manual authenticator key. No session
cookie or access token is issued for this intermediate response.

POST /api/v1/auth/mfa/complete accepts challengeToken/code/recovery and an
X-CSRF-Token header. It verifies the password-proof security stamp, expiry,
attempts, account lockout and purpose. Codes use six digits, SHA1, a 30-second
TOTP interval and a one-step network-delay window. Matched steps are accepted
once only. Five failed attempts close the challenge. Starting another password
challenge does not reset the account's failed-MFA count.

Successful enrollment enables MFA and returns ten one-time recovery codes.
Save them securely; the UI does not persist them and removes the view after
confirmation. Recovery codes are 43-character random values and stored as hashes.
Successful recovery changes the security stamp, invalidating earlier sessions.

The /login page supports password, manual-key enrollment, MFA verification and
recovery. Access tokens never enter localStorage/sessionStorage. Refresh and logout
are serialized using Web Locks across tabs; BroadcastChannel carries only
change/revocation signals. HTTPS is required for the session/CSRF cookies.

Current limits: MFA step-up, MFA management/rotation, invitations/registration,
reset/verification, immutable audit and session/workspace screens are unfinished.
Frontend E2E auth uses local mocked HTTP responses; real PostgreSQL verifies the
credential use cases separately. Full HTTPS browser-to-live-backend setup remains
unverified. Do not claim production security verification or ASVS conformance.