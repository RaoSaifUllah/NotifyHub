using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using NotifyHub.Infrastructure.Identity;
namespace NotifyHub.UnitTests;
public class JwtTests
{
    [Fact]
    public void SignedAccessTokenHasBoundedLifetimeAndRejectsInvalidClaimsAndTampering()
    {
        using var rsa = RSA.Create(3072);
        var issuer = new JwtTokenIssuer(rsa.ExportPkcs8PrivateKeyPem(), "notifyhub-test", "notifyhub-web");
        var user = Guid.NewGuid();
        var session = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var raw = issuer.Issue(user, session, now);
        var validation = new TokenValidationParameters {
            ValidateIssuerSigningKey = true, IssuerSigningKey = new RsaSecurityKey(rsa),
            ValidateIssuer = true, ValidIssuer = "notifyhub-test",
            ValidateAudience = true, ValidAudience = "notifyhub-web",
            ValidateLifetime = true, RequireExpirationTime = true, RequireSignedTokens = true,
            ClockSkew = TimeSpan.FromSeconds(15), ValidAlgorithms = [SecurityAlgorithms.RsaSha256], ValidTypes = ["at+jwt"]
        };
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(raw, validation, out var validated);
        Assert.Equal(user.ToString(), principal.FindFirst("sub")?.Value);
        Assert.Equal(session.ToString(), principal.FindFirst("sid")?.Value);
        Assert.Equal(TimeSpan.FromMinutes(5), validated.ValidTo - validated.ValidFrom);
        Assert.False(principal.HasClaim(x => x.Type == "role"));
        var invalidAudience = validation.Clone(); invalidAudience.ValidAudience = "other";
        Assert.ThrowsAny<SecurityTokenException>(() => handler.ValidateToken(raw, invalidAudience, out _));
        var invalidIssuer = validation.Clone(); invalidIssuer.ValidIssuer = "other";
        Assert.ThrowsAny<SecurityTokenException>(() => handler.ValidateToken(raw, invalidIssuer, out _));
        var invalidAlgorithm = validation.Clone(); invalidAlgorithm.ValidAlgorithms = [SecurityAlgorithms.HmacSha256];
        Assert.ThrowsAny<SecurityTokenException>(() => handler.ValidateToken(raw, invalidAlgorithm, out _));
        Assert.Throws<SecurityTokenExpiredException>(() => handler.ValidateToken(issuer.Issue(user, session, now.AddMinutes(-10)), validation, out _));
        var parts = raw.Split('.');
        parts[2] = (parts[2][0] == 'A' ? "B" : "A") + parts[2][1..];
        Assert.ThrowsAny<SecurityTokenException>(() => handler.ValidateToken(string.Join('.', parts), validation, out _));
    }
    [Fact]
    public void WeakSigningKeysAreRejected()
    {
        using var rsa = RSA.Create(2048);
        Assert.Throws<InvalidOperationException>(() => new JwtTokenIssuer(rsa.ExportPkcs8PrivateKeyPem(), "issuer", "audience").Issue(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow));
    }
    [Fact]
    public void OpaqueTokensHaveIndependentEntropyAndOnlyHashesAreCompared()
    {
        var secrets = new TokenSecrets();
        var first = secrets.Generate();
        var second = secrets.Generate();
        Assert.Equal(43, first.Length);
        Assert.NotEqual(first, second);
        Assert.Equal(64, secrets.Hash(first).Length);
        Assert.True(secrets.Matches(first, secrets.Hash(first)));
        Assert.False(secrets.Matches(second, secrets.Hash(first)));
        Assert.False(secrets.Matches(first, "invalid"));
    }
}
