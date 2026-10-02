using NotifyHub.Application.Workspaces;
using NotifyHub.Domain.Identity;
namespace NotifyHub.Application.Identity;
public sealed record UserSecurity(Guid Id, string SecurityStamp, bool Disabled);
public sealed record SessionCredentials(string AccessToken, string RefreshToken, string CsrfToken, Guid SessionId);
public interface ISessionRepository
{
    void Add(RefreshSession session, RefreshToken token);
    void Add(RefreshToken token);
    Task<(RefreshSession Session, RefreshToken Token)?> LockByTokenHashAsync(string hash, CancellationToken cancellationToken);
    Task<RefreshSession?> LockSessionAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<UserSecurity?> UserSecurityAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> IsActiveAsync(Guid userId, Guid sessionId, DateTimeOffset now, CancellationToken cancellationToken);
}
public interface ITokenSecrets
{
    string Generate();
    string Hash(string secret);
    bool Matches(string secret, string hash);
}
public interface IAccessTokenIssuer { string Issue(Guid userId, Guid sessionId, DateTimeOffset now); }
public sealed class SessionService(ISessionRepository repository, IUnitOfWork unit, ITokenSecrets secrets, IAccessTokenIssuer issuer, TimeProvider clock)
{
    public async Task<SessionCredentials> CreateAsync(Guid userId, CancellationToken cancellationToken)
    {
        SessionCredentials? credentials = null;
        await unit.ExecuteAsync(async token =>
        {
            var user = await repository.UserSecurityAsync(userId, token);
            if (user is null || user.Disabled) throw new UnauthorizedAccessException();
            var now = clock.GetUtcNow();
            var csrf = secrets.Generate();
            var raw = secrets.Generate();
            var session = RefreshSession.Create(userId, user.SecurityStamp, secrets.Hash(csrf), now);
            repository.Add(session, RefreshToken.Create(session.Id, secrets.Hash(raw), now.AddDays(7)));
            credentials = new(issuer.Issue(userId, session.Id, now), raw, csrf, session.Id);
        }, cancellationToken);
        return credentials!;
    }
    public async Task<SessionCredentials?> RefreshAsync(string rawToken, string csrfToken, CancellationToken cancellationToken)
    {
        if (rawToken.Length > 512 || csrfToken.Length > 512) return null;
        SessionCredentials? credentials = null;
        await unit.ExecuteAsync(async token =>
        {
            var match = await repository.LockByTokenHashAsync(secrets.Hash(rawToken), token);
            if (match is null) return;
            var (session, refresh) = match.Value;
            var now = clock.GetUtcNow();
            if (!secrets.Matches(csrfToken, session.CsrfHash)) return;
            // Reuse commits revocation before returning failure. Never throw inside this transaction.
            if (refresh.UsedAt is not null) { session.Revoke(now); return; }
            var user = await repository.UserSecurityAsync(session.UserId, token);
            if (!session.IsActive(now) || now >= refresh.ExpiresAt || user is null || user.Disabled || user.SecurityStamp != session.SecurityStamp)
            { session.Revoke(now); return; }
            var raw = secrets.Generate();
            var expiry = now.AddDays(7) < session.AbsoluteExpiresAt ? now.AddDays(7) : session.AbsoluteExpiresAt;
            var next = RefreshToken.Create(session.Id, secrets.Hash(raw), expiry, refresh.Id);
            refresh.Consume(next.Id, now);
            session.Touch(now);
            repository.Add(next);
            credentials = new(issuer.Issue(session.UserId, session.Id, now), raw, csrfToken, session.Id);
        }, cancellationToken);
        return credentials;
    }
    public async Task<bool> RevokeFromRefreshAsync(string rawToken, string csrfToken, CancellationToken cancellationToken)
    {
        if (rawToken.Length is < 1 or > 512 || csrfToken.Length is < 1 or > 512) return false;
        var revoked = false;
        await unit.ExecuteAsync(async token =>
        {
            var match = await repository.LockByTokenHashAsync(secrets.Hash(rawToken), token);
            if (match is null || !secrets.Matches(csrfToken, match.Value.Session.CsrfHash)) return;
            match.Value.Session.Revoke(clock.GetUtcNow());
            revoked = true;
        }, cancellationToken);
        return revoked;
    }
    public Task RevokeAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken) =>
        unit.ExecuteAsync(async token =>
        {
            var session = await repository.LockSessionAsync(sessionId, token);
            if (session is null || session.UserId != userId) throw new KeyNotFoundException();
            session.Revoke(clock.GetUtcNow());
        }, cancellationToken);
}
