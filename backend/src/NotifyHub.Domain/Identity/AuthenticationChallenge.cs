namespace NotifyHub.Domain.Identity;

public enum ChallengePurpose { EnrollMfa, VerifyMfa }

public sealed class AuthenticationChallenge
{
    private AuthenticationChallenge() { }
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = "";
    public string CsrfHash { get; private set; } = "";
    public string SecurityStamp { get; private set; } = "";
    public ChallengePurpose Purpose { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? ConsumedAt { get; private set; }
    public int Attempts { get; private set; }
    public uint Version { get; private set; }

    public static AuthenticationChallenge Create(Guid userId, string tokenHash, string csrfHash,
        string securityStamp, ChallengePurpose purpose, DateTimeOffset now)
    {
        if (userId == Guid.Empty || tokenHash.Length != 64 || csrfHash.Length != 64 ||
            string.IsNullOrWhiteSpace(securityStamp) || !Enum.IsDefined(purpose))
            throw new ArgumentException("Invalid authentication challenge.");
        return new() { Id = Guid.NewGuid(), UserId = userId, TokenHash = tokenHash,
            CsrfHash = csrfHash, SecurityStamp = securityStamp, Purpose = purpose, ExpiresAt = now.AddMinutes(5) };
    }
    public bool IsActive(DateTimeOffset now) => ConsumedAt is null && Attempts < 5 && now < ExpiresAt;
    public void Fail(DateTimeOffset now) { Attempts++; if (Attempts >= 5) Consume(now); }
    public void Consume(DateTimeOffset now) => ConsumedAt ??= now;
}