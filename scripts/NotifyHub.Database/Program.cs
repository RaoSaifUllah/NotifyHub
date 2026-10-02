using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NotifyHub.Infrastructure.Persistence;
try
{
    var path = Path.Combine(Directory.GetCurrentDirectory(), "backend/src/NotifyHub.Api/appsettings.Local.json");
    using var config = JsonDocument.Parse(File.ReadAllText(path));
    var values = config.RootElement.GetProperty("ConnectionStrings");
    var migration = Normalize(values.GetProperty("Migration").GetString()!);
    var write = Normalize(values.GetProperty("Write").GetString()!);
    var read = Normalize(values.GetProperty("Read").GetString()!);
    var m = new NpgsqlConnectionStringBuilder(migration);
    var w = new NpgsqlConnectionStringBuilder(write);
    var r = new NpgsqlConnectionStringBuilder(read);
    Console.WriteLine($"Distinct users: {new[] { m.Username, w.Username, r.Username }.Distinct().Count() == 3}");
    Console.WriteLine($"Same database: {m.Database == w.Database && m.Database == r.Database}");
    Console.WriteLine($"Test database suffix: {m.Database?.EndsWith("_test") == true}");
    await using var connection = new NpgsqlConnection(migration);
    await connection.OpenAsync();
    await using var command = new NpgsqlCommand("SELECT current_setting('server_version'), (SELECT rolsuper FROM pg_roles WHERE rolname = current_user), (SELECT rolcreaterole FROM pg_roles WHERE rolname = current_user), (SELECT count(*) FROM information_schema.tables WHERE table_schema='public' AND table_name NOT IN ('__EFMigrationsHistory','workspaces','workspace_summaries','workspace_members','AspNetUsers','AspNetRoles','AspNetUserClaims','AspNetUserLogins','AspNetUserRoles','AspNetUserTokens','AspNetRoleClaims','refresh_sessions','refresh_tokens','authentication_challenges'))", connection);
    await using var reader = await command.ExecuteReaderAsync();
    await reader.ReadAsync();
    var unrelatedTables = reader.GetInt64(3);
    Console.WriteLine($"PostgreSQL version: {reader.GetString(0)}; migration superuser: {reader.GetBoolean(1)}; can create roles: {reader.GetBoolean(2)}; unrelated tables: {unrelatedTables}");
    await reader.DisposeAsync();
    if (args.Contains("migrate") || args.Contains("setup"))
    {
        if (unrelatedTables != 0) throw new InvalidOperationException("Unrelated database tables.");
        await using var db = new NotifyHubDbContext(new DbContextOptionsBuilder<NotifyHubDbContext>().UseNpgsql(migration).Options);
        await db.Database.MigrateAsync();
        Console.WriteLine("Migration applied.");
    }
    if (args.Contains("grants"))
    {
        await using var grants = new NpgsqlCommand(File.ReadAllText("deploy/database-roles.sql"), connection);
        await grants.ExecuteNonQueryAsync();
        Console.WriteLine("Reviewed grants applied.");
    }
    if (args.Contains("setup"))
    {
        foreach (var role in new[] { "notifyhub_write", "notifyhub_read" })
        {
            await using var exists = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM pg_roles WHERE rolname=@role)", connection);
            exists.Parameters.AddWithValue("role", role);
            if ((bool)(await exists.ExecuteScalarAsync())!) throw new InvalidOperationException("Target role already exists; refusing to replace its credentials.");
        }
        var writePassword = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var readPassword = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        await using var create = new NpgsqlCommand(
            $"CREATE ROLE notifyhub_write LOGIN PASSWORD '{writePassword}' NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT; CREATE ROLE notifyhub_read LOGIN PASSWORD '{readPassword}' NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT;", connection);
        await create.ExecuteNonQueryAsync();
        await using var grants = new NpgsqlCommand(File.ReadAllText("deploy/database-roles.sql"), connection);
        await grants.ExecuteNonQueryAsync();
        w.Username = "notifyhub_write"; w.Password = writePassword;
        r.Username = "notifyhub_read"; r.Password = readPassword;
        var document = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))!;
        document["ConnectionStrings"]!["Write"] = w.ConnectionString;
        document["ConnectionStrings"]!["Read"] = r.ConnectionString;
        document["ConnectionStrings"]!["Migration"] = m.ConnectionString;
        File.WriteAllText(path, document.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("Restricted roles provisioned; credentials saved only to ignored local configuration.");
    }
}
catch (Exception exception)
{
    Console.WriteLine($"Database preflight failed: {exception.GetType().Name}. Details suppressed to protect credentials.");
    Environment.ExitCode = 1;
}
static string Normalize(string input)
{
    if (!input.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) && !input.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)) return input;
    var uri = new Uri(input);
    var user = uri.UserInfo.Split(':', 2);
    return new NpgsqlConnectionStringBuilder {
        Host = uri.Host, Port = uri.IsDefaultPort ? 5432 : uri.Port,
        Username = Uri.UnescapeDataString(user[0]), Password = user.Length > 1 ? Uri.UnescapeDataString(user[1]) : "",
        Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
        SslMode = SslMode.VerifyFull, Timeout = 10
    }.ConnectionString;
}






