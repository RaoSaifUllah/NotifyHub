using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using NotifyHub.Application.Identity;
namespace NotifyHub.Infrastructure.Identity;
public sealed class TokenSecrets : ITokenSecrets
{
    public string Generate() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
    public string Hash(string secret) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
    public bool Matches(string secret, string hash)
    {
        if (hash.Length != 64) return false;
        try { return CryptographicOperations.FixedTimeEquals(Convert.FromHexString(Hash(secret)), Convert.FromHexString(hash)); }
        catch (FormatException) { return false; }
    }
}
