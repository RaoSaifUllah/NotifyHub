using Npgsql;
using NotifyHub.Infrastructure.Persistence;
namespace NotifyHub.UnitTests;
public class DatabaseConnectionTests
{
    [Fact]
    public void ParsesUrlAndEnforcesVerifiedTls()
    {
        var parsed = new NpgsqlConnectionStringBuilder(DatabaseConnection.Normalize("postgresql://test_user:test%40value@db.example.test/notifyhub?sslmode=require"));
        Assert.Equal("test@value", parsed.Password);
        Assert.Equal("notifyhub", parsed.Database);
        Assert.Equal(SslMode.VerifyFull, parsed.SslMode);
        Assert.Equal(5432, parsed.Port);
    }
    [Fact]
    public void InvalidInputCannotLeakSecretThroughException()
    {
        const string input = "Password=do-not-print;InvalidSetting=value";
        var exception = Assert.Throws<InvalidOperationException>(() => DatabaseConnection.Normalize(input));
        Assert.DoesNotContain("do-not-print", exception.ToString());
        Assert.Null(exception.InnerException);
    }
    [Fact]
    public void EmptyConnectionIsUnconfigured() => Assert.Equal("", DatabaseConnection.Normalize(""));
}
