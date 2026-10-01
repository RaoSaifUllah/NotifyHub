using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NotifyHub.Infrastructure.Persistence;
namespace NotifyHub.Infrastructure.Identity;
public sealed class SecureUserStore(NotifyHubDbContext db, IDataProtectionProvider protection)
    : UserStore<AccountUser, IdentityRole<Guid>, NotifyHubDbContext, Guid>(db)
{
    private IDataProtector Protector(AccountUser user) =>
        protection.CreateProtector("NotifyHub.Identity.Authenticator.v1").CreateProtector(user.Id.ToString());
    public override Task SetAuthenticatorKeyAsync(AccountUser user, string key, CancellationToken cancellationToken) =>
        base.SetAuthenticatorKeyAsync(user, "DP1:" + Protector(user).Protect(key), cancellationToken);
    public override async Task<string?> GetAuthenticatorKeyAsync(AccountUser user, CancellationToken cancellationToken)
    {
        var encrypted = await base.GetAuthenticatorKeyAsync(user, cancellationToken);
        if (encrypted is null) return null;
        if (!encrypted.StartsWith("DP1:", StringComparison.Ordinal)) throw new InvalidOperationException("Invalid authenticator storage.");
        try { return Protector(user).Unprotect(encrypted[4..]); }
        catch (CryptographicException) { throw new InvalidOperationException("Authenticator key could not be decrypted."); }
    }
    public override Task ReplaceCodesAsync(AccountUser user, IEnumerable<string> recoveryCodes, CancellationToken cancellationToken)
    {
        var codes = recoveryCodes.ToArray();
        if (codes.Length > 10 || codes.Any(x => x.Length < 32 || x.Length > 128))
            throw new ArgumentException("Recovery codes must be high-entropy values.");
        var secrets = new TokenSecrets();
        return base.ReplaceCodesAsync(user, codes.Select(secrets.Hash), cancellationToken);
    }
    public override async Task<bool> RedeemCodeAsync(AccountUser user, string code, CancellationToken cancellationToken)
    {
        if (code.Length > 128) return false;
        if (Context.Database.CurrentTransaction is null) throw new InvalidOperationException("Recovery redemption requires a transaction.");
        // Serialize redemption on the account, and reload token state after waiting.
        await Context.Users.FromSqlInterpolated($"SELECT * FROM \"AspNetUsers\" WHERE \"Id\" = {user.Id} FOR UPDATE").SingleAsync(cancellationToken);
        var token = await Context.UserTokens.SingleOrDefaultAsync(x => x.UserId == user.Id && x.LoginProvider == "[AspNetUserStore]" && x.Name == "RecoveryCodes", cancellationToken);
        if (token is null) return false;
        await Context.Entry(token).ReloadAsync(cancellationToken);
        var hashes = (token.Value ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries);
        var secrets = new TokenSecrets();
        var match = hashes.FirstOrDefault(hash => secrets.Matches(code, hash));
        if (match is null) return false;
        // Call base to preserve existing hashes rather than hashing them again.
        await base.ReplaceCodesAsync(user, hashes.Where(hash => hash != match), cancellationToken);
        return true;
    }
}

