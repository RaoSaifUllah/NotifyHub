using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
namespace NotifyHub.Infrastructure.Identity;
public sealed class Argon2PasswordHasher : IPasswordHasher<AccountUser>
{
    public const int MemoryKiB = 65536;
    public const int Iterations = 3;
    private static readonly SemaphoreSlim WorkSlots = new(2);
    public string HashPassword(AccountUser user, string password)
    {
        if (Encoding.UTF8.GetByteCount(password) > 1024) throw new ArgumentException("Password exceeds the supported limit.");
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Derive(password, salt, MemoryKiB, Iterations, 1);
        return "$argon2id$v=19$m=" + MemoryKiB + ",t=" + Iterations + ",p=1$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(hash);
    }
    public PasswordVerificationResult VerifyHashedPassword(AccountUser user, string hashedPassword, string providedPassword)
    {
        if (hashedPassword.Length > 512 || Encoding.UTF8.GetByteCount(providedPassword) > 1024)
            return PasswordVerificationResult.Failed;
        try
        {
            var parts = hashedPassword.Split('$');
            if (parts.Length != 6 || parts[1] != "argon2id" || parts[2] != "v=19") return PasswordVerificationResult.Failed;
            var parameters = parts[3].Split(',');
            if (parameters.Length != 3 || !parameters[0].StartsWith("m=") || !parameters[1].StartsWith("t=") || !parameters[2].StartsWith("p="))
                return PasswordVerificationResult.Failed;
            var memory = int.Parse(parameters[0][2..], CultureInfo.InvariantCulture);
            var iterations = int.Parse(parameters[1][2..], CultureInfo.InvariantCulture);
            var parallelism = int.Parse(parameters[2][2..], CultureInfo.InvariantCulture);
            if (memory < 19456 || memory > MemoryKiB || iterations < 2 || iterations > 5 || parallelism < 1 || parallelism > 4)
                return PasswordVerificationResult.Failed;
            var salt = Convert.FromBase64String(parts[4]);
            var expected = Convert.FromBase64String(parts[5]);
            if (salt.Length != 16 || expected.Length != 32) return PasswordVerificationResult.Failed;
            var actual = Derive(providedPassword, salt, memory, iterations, parallelism);
            if (!CryptographicOperations.FixedTimeEquals(actual, expected)) return PasswordVerificationResult.Failed;
            return memory < MemoryKiB || iterations < Iterations ? PasswordVerificationResult.SuccessRehashNeeded : PasswordVerificationResult.Success;
        }
        catch (FormatException) { return PasswordVerificationResult.Failed; }
        catch (OverflowException) { return PasswordVerificationResult.Failed; }
    }
    private static byte[] Derive(string password, byte[] salt, int memory, int iterations, int parallelism)
    {
        var bytes = Encoding.UTF8.GetBytes(password);
        WorkSlots.Wait();
        try
        {
            using var argon = new Argon2id(bytes) { Salt = salt, MemorySize = memory, Iterations = iterations, DegreeOfParallelism = parallelism };
            return argon.GetBytes(32);
        }
        finally { CryptographicOperations.ZeroMemory(bytes); WorkSlots.Release(); }
    }
}
