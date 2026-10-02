using NotifyHub.Domain.Identity;
namespace NotifyHub.UnitTests;

public sealed class AuthenticationChallengeTests
{
    [Fact]
    public void ChallengeExpiresAndConsumesPermanently()
    {
        var now = new DateTimeOffset(2026, 10, 2, 0, 0, 0, TimeSpan.Zero);
        var challenge = AuthenticationChallenge.Create(Guid.NewGuid(), new string('A', 64), new string('B', 64),
            "stamp", ChallengePurpose.EnrollMfa, now);
        Assert.True(challenge.IsActive(now.AddMinutes(4)));
        Assert.False(challenge.IsActive(now.AddMinutes(5)));
        challenge.Consume(now.AddMinutes(1));
        challenge.Consume(now.AddMinutes(2));
        Assert.Equal(now.AddMinutes(1), challenge.ConsumedAt);
        Assert.False(challenge.IsActive(now));
    }

    [Fact]
    public void FiveFailuresCloseChallenge()
    {
        var now = DateTimeOffset.UtcNow;
        var challenge = AuthenticationChallenge.Create(Guid.NewGuid(), new string('A', 64), new string('B', 64),
            "stamp", ChallengePurpose.VerifyMfa, now);
        for (var i = 0; i < 4; i++) challenge.Fail(now);
        Assert.True(challenge.IsActive(now));
        challenge.Fail(now);
        Assert.False(challenge.IsActive(now));
        Assert.NotNull(challenge.ConsumedAt);
    }
}