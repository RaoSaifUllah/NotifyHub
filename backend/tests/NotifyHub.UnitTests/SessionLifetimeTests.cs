using NotifyHub.Domain.Identity;
namespace NotifyHub.UnitTests;
public class SessionLifetimeTests
{
    [Fact]
    public void IdleAndAbsoluteBoundariesAreEnforced()
    {
        var now = new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
        var session = RefreshSession.Create(Guid.NewGuid(), "stamp", new string('A', 64), now);
        Assert.True(session.IsActive(now));
        Assert.False(session.IsActive(now.AddDays(7)));
        session.Touch(now.AddDays(6));
        Assert.True(session.IsActive(now.AddDays(12)));
        session.Touch(now.AddDays(29));
        Assert.False(session.IsActive(now.AddDays(30)));
    }
    [Fact]
    public void RevocationIsPermanentAndPreservesFirstRevocationTime()
    {
        var now = DateTimeOffset.UtcNow;
        var session = RefreshSession.Create(Guid.NewGuid(), "stamp", new string('A', 64), now);
        session.Revoke(now);
        session.Revoke(now.AddMinutes(1));
        Assert.Equal(now, session.RevokedAt);
        session.Touch(now.AddMinutes(2));
        Assert.False(session.IsActive(now.AddMinutes(2)));
    }
}
