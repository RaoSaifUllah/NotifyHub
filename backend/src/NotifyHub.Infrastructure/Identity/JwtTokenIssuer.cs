using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using NotifyHub.Application.Identity;
namespace NotifyHub.Infrastructure.Identity;
public sealed class JwtTokenIssuer(string privatePem, string issuer, string audience) : IAccessTokenIssuer
{
    public string Issue(Guid userId, Guid sessionId, DateTimeOffset now)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(privatePem);
        if (rsa.KeySize < 3072) throw new InvalidOperationException("JWT signing key must be at least 3072 bits.");
        var key = new RsaSecurityKey(rsa) { CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }, KeyId = Convert.ToHexString(SHA256.HashData(rsa.ExportSubjectPublicKeyInfo()))[..16] };
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer, Audience = audience,
            Subject = new ClaimsIdentity([new Claim("sub", userId.ToString()), new Claim("sid", sessionId.ToString())]),
            IssuedAt = now.UtcDateTime, NotBefore = now.UtcDateTime, Expires = now.AddMinutes(5).UtcDateTime,
            TokenType = "at+jwt", SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256)
        };
        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }
}

