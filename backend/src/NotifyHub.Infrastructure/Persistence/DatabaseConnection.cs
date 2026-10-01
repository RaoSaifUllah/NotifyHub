using Npgsql;
namespace NotifyHub.Infrastructure.Persistence;
public static class DatabaseConnection
{
    public static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        try
        {
            if (!input.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) && !input.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
                return new NpgsqlConnectionStringBuilder(input).ConnectionString;
            var uri = new Uri(input);
            var user = uri.UserInfo.Split(':', 2);
            return new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.IsDefaultPort ? 5432 : uri.Port,
                Username = Uri.UnescapeDataString(user[0]),
                Password = user.Length > 1 ? Uri.UnescapeDataString(user[1]) : "",
                Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
                SslMode = SslMode.VerifyFull,
                Timeout = 10
            }.ConnectionString;
        }
        catch
        {
            // Never attach the parser exception: it can include the raw credential.
            throw new InvalidOperationException("Invalid PostgreSQL connection configuration.");
        }
    }
}
