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
MFA enrollment/login/step-up flows are still pending; setup alone does not grant
a usable privileged browser session. Do not treat this as a production-ready
authentication installation.

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
Set-Cookie, never JSON.

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
does not replace login. Public credential login, enrollment, invites and
verification/reset challenges are still pending, so browser refresh cannot be
initiated from the current UI.

## Verification

    dotnet build NotifyHub.slnx --no-restore --verbosity quiet
    dotnet test backend/tests/NotifyHub.IntegrationTests --filter FullyQualifiedName~BrowserSessionTests
    ./scripts/Test-PostgreSql.ps1

The HTTP fixture verifies transport/cookie contracts against real SessionService
logic with a controlled repository. The PostgreSQL fixture separately verifies
actual locks, bootstrap concurrency, rollback, refresh races and cookie-logout
revocation. No fixture bootstraps a real operator account or sends notifications.