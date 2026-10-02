using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NotifyHub.Application.Identity;
using NotifyHub.Application.Workspaces;
using NotifyHub.Domain.Identity;
using NotifyHub.Domain.Workspaces;
using NotifyHub.Infrastructure.Persistence;
using OtpNet;

namespace NotifyHub.Infrastructure.Identity;

public sealed class CredentialLogin(NotifyHubDbContext db, UserManager<AccountUser> users,
    IUserStore<AccountUser> store, IPasswordHasher<AccountUser> hasher,
    IUnitOfWork unit, ITokenSecrets secrets, IAccessTokenIssuer issuer, TimeProvider clock) : ICredentialLogin
{
    private static readonly Lazy<string> DummyHash = new(() =>
        new Argon2PasswordHasher().HashPassword(new AccountUser(), new TokenSecrets().Generate()));

    public async Task<LoginStart?> BeginAsync(string email, string password, CancellationToken cancellationToken)
    {
        if (email.Length is < 3 or > 254 || password.Length is < 1 or > 256) return null;
        LoginStart? result = null;
        await unit.ExecuteAsync(async token =>
        {
            var normalized = users.NormalizeEmail(email.Trim());
            var user = await db.Users.FromSqlInterpolated(
                $"SELECT * FROM \"AspNetUsers\" WHERE \"NormalizedEmail\" = {normalized} FOR UPDATE")
                .SingleOrDefaultAsync(token);
            if (user is null)
            {
                hasher.VerifyHashedPassword(new AccountUser(), DummyHash.Value, password);
                return;
            }
            await db.Entry(user).ReloadAsync(token);
            if (await users.IsLockedOutAsync(user) || user.Disabled || !user.EmailConfirmed)
            {
                hasher.VerifyHashedPassword(user, DummyHash.Value, password);
                return;
            }
            if (!await users.CheckPasswordAsync(user, password))
            {
                await RequireSuccess(users.AccessFailedAsync(user));
                return;
            }
            var now = clock.GetUtcNow();
            var requiresMfa = user.IsSystemAdministrator ||
                await db.Set<WorkspaceMember>().AnyAsync(x => x.UserId == user.Id && x.Role == WorkspaceRole.Owner, token);
            if (!user.TwoFactorEnabled && !requiresMfa)
            {
                await RequireSuccess(users.ResetAccessFailedCountAsync(user));
                result = new("Authenticated", null, null, null, Issue(user, now, false));
                return;
            }
            var purpose = user.TwoFactorEnabled ? ChallengePurpose.VerifyMfa : ChallengePurpose.EnrollMfa;
            string? key = null;
            if (purpose == ChallengePurpose.EnrollMfa)
            {
                await RequireSuccess(users.ResetAuthenticatorKeyAsync(user));
                await RequireSuccess(users.UpdateSecurityStampAsync(user));
                user.LastTotpStep = null;
                key = await users.GetAuthenticatorKeyAsync(user);
            }
            var raw = secrets.Generate();
            var csrf = secrets.Generate();
            db.Set<AuthenticationChallenge>().Add(AuthenticationChallenge.Create(user.Id,
                secrets.Hash(raw), secrets.Hash(csrf), user.SecurityStamp!, purpose, now));
            result = new(purpose.ToString(), raw, csrf, key, null);
        }, cancellationToken);
        return result;
    }

    public async Task<LoginCompletion?> CompleteMfaAsync(string challengeToken, string csrfToken, string code,
        bool recovery, CancellationToken cancellationToken)
    {
        if (challengeToken.Length != 43 || csrfToken.Length != 43 || code.Length > 128) return null;
        LoginCompletion? result = null;
        await unit.ExecuteAsync(async token =>
        {
            var challenge = await db.Set<AuthenticationChallenge>()
                .SingleOrDefaultAsync(x => x.TokenHash == secrets.Hash(challengeToken), token);
            if (challenge is null || !secrets.Matches(csrfToken, challenge.CsrfHash)) return;
            // Every credential/challenge mutation locks account first. Reload after waiting.
            var user = await db.Users.FromSqlInterpolated(
                $"SELECT * FROM \"AspNetUsers\" WHERE \"Id\" = {challenge.UserId} FOR UPDATE").SingleOrDefaultAsync(token);
            if (user is null) return;
            await db.Entry(user).ReloadAsync(token);
            await db.Entry(challenge).ReloadAsync(token);
            var now = clock.GetUtcNow();
            if (!challenge.IsActive(now) || challenge.SecurityStamp != user.SecurityStamp ||
                user.Disabled || !user.EmailConfirmed || await users.IsLockedOutAsync(user)) return;
            if ((challenge.Purpose == ChallengePurpose.EnrollMfa && user.TwoFactorEnabled) ||
                (challenge.Purpose == ChallengePurpose.VerifyMfa && !user.TwoFactorEnabled)) return;
            var valid = false;
            if (recovery && challenge.Purpose == ChallengePurpose.VerifyMfa)
            {
                valid = await ((IUserTwoFactorRecoveryCodeStore<AccountUser>)store).RedeemCodeAsync(user, code, token);
            }
            else if (!recovery && code.Length == 6 && code.All(char.IsAsciiDigit))
            {
                var key = await users.GetAuthenticatorKeyAsync(user);
                if (key is not null)
                {
                    var otp = new Totp(Base32Encoding.ToBytes(key));
                    valid = otp.VerifyTotp(now.UtcDateTime, code, out var step, VerificationWindow.RfcSpecifiedNetworkDelay)
                        && (user.LastTotpStep is null || step > user.LastTotpStep);
                    if (valid) user.LastTotpStep = step;
                }
            }
            if (!valid)
            {
                challenge.Fail(now);
                await RequireSuccess(users.AccessFailedAsync(user));
                return;
            }
            challenge.Consume(now);
            await RequireSuccess(users.ResetAccessFailedCountAsync(user));
            string[] codes = [];
            if (challenge.Purpose == ChallengePurpose.EnrollMfa)
            {
                await RequireSuccess(users.SetTwoFactorEnabledAsync(user, true));
                await RequireSuccess(users.UpdateSecurityStampAsync(user));
                codes = Enumerable.Range(0, 10).Select(_ => secrets.Generate()).ToArray();
                await ((IUserTwoFactorRecoveryCodeStore<AccountUser>)store).ReplaceCodesAsync(user, codes, token);
            }
            else if (recovery)
            {
                // Recovery invalidates earlier sessions and outstanding challenges.
                await RequireSuccess(users.UpdateSecurityStampAsync(user));
            }
            result = new(Issue(user, now, true), codes);
        }, cancellationToken);
        return result;
    }

    private SessionCredentials Issue(AccountUser user, DateTimeOffset now, bool mfa)
    {
        var csrf = secrets.Generate();
        var raw = secrets.Generate();
        var session = RefreshSession.Create(user.Id, user.SecurityStamp!, secrets.Hash(csrf), now, mfa ? now : null);
        db.Set<RefreshSession>().Add(session);
        db.Set<RefreshToken>().Add(RefreshToken.Create(session.Id, secrets.Hash(raw), now.AddDays(7)));
        return new(issuer.Issue(user.Id, session.Id, now), raw, csrf, session.Id);
    }
    private static async Task RequireSuccess(Task<IdentityResult> operation)
    {
        if (!(await operation).Succeeded) throw new InvalidOperationException("Account update failed.");
    }
}