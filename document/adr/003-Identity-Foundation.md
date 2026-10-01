# ADR-003: Identity lifecycle and protected authentication state
Date: 2026-10-01. Status: foundation implemented; public lifecycle flows pending.
Use ASP.NET Core Identity 10.0.12 for lifecycle/store contracts. Identity types stay
in Infrastructure. Disable store AutoSaveChanges so application Unit of Work owns
transactions. Membership mutations lock the workspace before owner checks.
Use Konscious Argon2id 1.3.1 through IPasswordHasher: 64MiB/t=3/p=1, random 16-byte
salt, fixed-time comparison, bounded stored-hash parameters and two concurrent workers.
Initial host timing was 1019ms (80 logical processors), requiring later load measurement.
Use five-minute RS256 tokens, RSA >=3072 bits, kid from public-key fingerprint.
JWT issuance tests reject issuer/audience/algorithm/expiry/tampering; API validation
and key rotation are still pending. Per-call keys disable provider caching to avoid
retaining disposed RSA objects; regression tests found and verified that fix.
Use opaque 256-bit refresh/CSRF secrets and SHA-256 hashes. Serialize on session row
for rotation, reload token after lock, commit replay revocation even on failed refresh.
Concurrent reuse revokes the session; there is no grace window. Password/security
stamp change is checked against active sessions through the write database.
Use ASP.NET Data Protection to encrypt TOTP secrets with per-account purpose binding.
Store high-entropy recovery code hashes; redemption locks account and removes one
hash atomically. Production key-ring protection and backup are required before release.
References:
- https://github.com/kmaragon/Konscious.Security.Cryptography
- https://github.com/dotnet/aspnetcore/blob/main/src/Identity/Extensions.Stores/src/UserStoreBase.cs
- https://learn.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model
No certification or full ASVS conformance is claimed.
