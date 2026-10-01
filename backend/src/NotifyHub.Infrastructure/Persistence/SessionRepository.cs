using Microsoft.EntityFrameworkCore;
using NotifyHub.Application.Identity;
using NotifyHub.Domain.Identity;
namespace NotifyHub.Infrastructure.Persistence;
public sealed class SessionRepository(NotifyHubDbContext db) : ISessionRepository
{
    public void Add(RefreshSession session, RefreshToken token) { db.Set<RefreshSession>().Add(session); db.Set<RefreshToken>().Add(token); }
    public void Add(RefreshToken token) => db.Set<RefreshToken>().Add(token);
    public async Task<(RefreshSession Session, RefreshToken Token)?> LockByTokenHashAsync(string hash, CancellationToken cancellationToken)
    {
        var refresh = await db.Set<RefreshToken>().SingleOrDefaultAsync(x => x.Hash == hash, cancellationToken);
        if (refresh is null) return null;
        var session = await LockSessionAsync(refresh.SessionId, cancellationToken);
        if (session is null) return null;
        await db.Entry(refresh).ReloadAsync(cancellationToken);
        return (session, refresh);
    }
    public async Task<RefreshSession?> LockSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("A transaction is required for session mutation.");
        var session = await db.Set<RefreshSession>().FromSqlInterpolated($"SELECT *, xmin FROM refresh_sessions WHERE \"Id\" = {sessionId} FOR UPDATE").SingleOrDefaultAsync(cancellationToken);
        if (session is not null) await db.Entry(session).ReloadAsync(cancellationToken);
        return session;
    }
    public Task<UserSecurity?> UserSecurityAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Users.Where(x => x.Id == userId).Select(x => new UserSecurity(x.Id, x.SecurityStamp!, x.Disabled)).SingleOrDefaultAsync(cancellationToken);
    public Task<bool> IsActiveAsync(Guid userId, Guid sessionId, DateTimeOffset now, CancellationToken cancellationToken) =>
        (from session in db.Set<RefreshSession>() join user in db.Users on session.UserId equals user.Id
         where session.Id == sessionId && user.Id == userId && session.RevokedAt == null && !user.Disabled &&
               session.AbsoluteExpiresAt > now && session.LastSeenAt > now.AddDays(-7) && session.SecurityStamp == user.SecurityStamp
         select session.Id).AnyAsync(cancellationToken);
}
