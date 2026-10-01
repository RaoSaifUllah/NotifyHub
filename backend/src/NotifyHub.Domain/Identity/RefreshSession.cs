namespace NotifyHub.Domain.Identity;
public sealed class RefreshSession
{
    private RefreshSession() { }
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string SecurityStamp { get; private set; } = "";
    public string CsrfHash { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset LastSeenAt { get; private set; }
    public DateTimeOffset AbsoluteExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public uint Version { get; private set; }
    public static RefreshSession Create(Guid userId, string stamp, string csrfHash, DateTimeOffset now) =>
        new() { Id = Guid.NewGuid(), UserId = userId, SecurityStamp = stamp, CsrfHash = csrfHash, CreatedAt = now, LastSeenAt = now, AbsoluteExpiresAt = now.AddDays(30) };
    public bool IsActive(DateTimeOffset now) => RevokedAt is null && now < AbsoluteExpiresAt && now < LastSeenAt.AddDays(7);
    public void Revoke(DateTimeOffset now) => RevokedAt ??= now;
    public void Touch(DateTimeOffset now) => LastSeenAt = now;
}
public sealed class RefreshToken
{
    private RefreshToken() { }
    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public string Hash { get; private set; } = "";
    public Guid? ParentId { get; private set; }
    public Guid? ReplacementId { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }
    public static RefreshToken Create(Guid sessionId, string hash, DateTimeOffset expiresAt, Guid? parentId = null) =>
        new() { Id = Guid.NewGuid(), SessionId = sessionId, Hash = hash, ExpiresAt = expiresAt, ParentId = parentId };
    public void Consume(Guid replacementId, DateTimeOffset now) { ReplacementId = replacementId; UsedAt = now; }
}
