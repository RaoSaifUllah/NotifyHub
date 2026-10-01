using Microsoft.AspNetCore.DataProtection;
using OtpNet;
using NotifyHub.Application.Identity;
using NotifyHub.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NotifyHub.Application.Workspaces;
using NotifyHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NotifyHub.Domain.Workspaces;
using NotifyHub.Infrastructure.Persistence;
namespace NotifyHub.IntegrationTests;
public class PostgreSqlTests
{
    [Fact]
    public async Task MigrationsRollbackScopedReadsAndDatabasePermissions()
    {
        var migration = DatabaseConnection.Normalize(Required("NOTIFYHUB_TEST_MIGRATION"));
        var write = DatabaseConnection.Normalize(Required("NOTIFYHUB_TEST_WRITE"));
        var read = DatabaseConnection.Normalize(Required("NOTIFYHUB_TEST_READ"));
        var mb = new NpgsqlConnectionStringBuilder(migration);
        var wb = new NpgsqlConnectionStringBuilder(write);
        var rb = new NpgsqlConnectionStringBuilder(read);
        Assert.Equal(mb.Database, wb.Database);
        Assert.Equal(mb.Database, rb.Database);
        Assert.Equal(3, new[] { mb.Username, wb.Username, rb.Username }.Distinct().Count());
        // Every run owns one unique schema. No existing database/schema is dropped.
        mb.Host = mb.Host?.Replace("-pooler.", "."); wb.Host = wb.Host?.Replace("-pooler.", "."); rb.Host = rb.Host?.Replace("-pooler.", ".");
        migration = mb.ConnectionString;
        var schema = "notifyhub_test_" + Guid.NewGuid().ToString("N");
        await using var owner = new NpgsqlConnection(migration);
        await owner.OpenAsync();
        await Execute(owner, $"CREATE SCHEMA {schema}");
        mb.SearchPath = schema; wb.SearchPath = schema; rb.SearchPath = schema;
        try
        {
            await using var admin = Context(mb.ConnectionString);
            await admin.Database.OpenConnectionAsync();
            await admin.Database.ExecuteSqlInterpolatedAsync($"SELECT set_config('search_path', {schema}, false)");
            await admin.Database.MigrateAsync();
            Assert.Empty(await admin.Database.GetPendingMigrationsAsync());
            var quote = new NpgsqlCommandBuilder();
            await Execute(owner, $"GRANT USAGE ON SCHEMA {schema} TO {quote.QuoteIdentifier(wb.Username!)}, {quote.QuoteIdentifier(rb.Username!)}");
            await Execute(owner, $"GRANT SELECT, INSERT, UPDATE, DELETE ON {schema}.workspaces TO {quote.QuoteIdentifier(wb.Username!)}");
            await Execute(owner, $"GRANT SELECT ON {schema}.workspace_summaries TO {quote.QuoteIdentifier(rb.Username!)}");
            foreach (var table in new[] { "AspNetUsers", "AspNetRoles", "AspNetUserClaims", "AspNetUserLogins", "AspNetUserRoles", "AspNetUserTokens", "AspNetRoleClaims", "workspace_members", "refresh_sessions", "refresh_tokens" })
                await Execute(owner, $"GRANT SELECT, INSERT, UPDATE, DELETE ON {schema}.{quote.QuoteIdentifier(table)} TO {quote.QuoteIdentifier(wb.Username!)}");
            await using var db = Context(wb.ConnectionString);
            await db.Database.OpenConnectionAsync();
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT set_config('search_path', {schema}, false)");
            var repository = new WorkspaceRepository(db);
            var unit = new UnitOfWork(db);
            var workspace = Workspace.Create("Integration fixture", "UTC", DateTimeOffset.UtcNow);
            await Assert.ThrowsAsync<InvalidOperationException>(() => unit.ExecuteAsync(async token =>
            {
                repository.Add(workspace);
                await unit.SaveChangesAsync(token);
                throw new InvalidOperationException("Injected failure after flush");
            }, CancellationToken.None));
            Assert.Null(await repository.FindAsync(workspace.Id, CancellationToken.None));
            await unit.ExecuteAsync(token =>
            {
                repository.Add(workspace);
                return Task.CompletedTask;
            }, CancellationToken.None);
            var services = new ServiceCollection();
            services.AddLogging();
            var keyDirectory = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestResults", schema, "identity-keys"));
            var protection = DataProtectionProvider.Create(keyDirectory);
            services.AddSingleton<IDataProtectionProvider>(protection);
            services.AddScoped(_ => db);
            services.AddIdentityCore<AccountUser>(options => { options.User.RequireUniqueEmail = true; options.Password.RequiredLength = 12; }).AddEntityFrameworkStores<NotifyHubDbContext>().AddTokenProvider<AuthenticatorTokenProvider<AccountUser>>(TokenOptions.DefaultAuthenticatorProvider);
            services.AddScoped<IUserStore<AccountUser>>(_ => new SecureUserStore(db, protection) { AutoSaveChanges = false });
            services.AddScoped<IPasswordHasher<AccountUser>, Argon2PasswordHasher>();
            await using var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AccountUser>>();
            var accounts = new AccountRepository(users);
            var creation = await accounts.CreateAsync("owner@notifyhub.example", "Local-Only-Test-Password1!", DateTimeOffset.UtcNow, CancellationToken.None);
            Assert.True(creation.Succeeded);
            Assert.False(await admin.Users.AnyAsync(x => x.Id == creation.UserId));
            await unit.SaveChangesAsync(CancellationToken.None);
            var persistedUser = await admin.Users.AsNoTracking().SingleAsync(x => x.Id == creation.UserId);
            Assert.StartsWith("$argon2id$", persistedUser.PasswordHash);
            Assert.True(await users.CheckPasswordAsync(await users.FindByIdAsync(creation.UserId.ToString()) ?? throw new InvalidOperationException(), "Local-Only-Test-Password1!"));
            var duplicate = await accounts.CreateAsync("OWNER@notifyhub.example", "Local-Only-Test-Password1!", DateTimeOffset.UtcNow, CancellationToken.None);
            Assert.False(duplicate.Succeeded);
            var members = new WorkspaceMembershipRepository(db);
            members.Add(WorkspaceMember.Create(workspace.Id, creation.UserId, WorkspaceRole.Owner));
            await unit.SaveChangesAsync(CancellationToken.None);
            var membership = new MembershipService(repository, members, unit);
            await Assert.ThrowsAsync<InvalidOperationException>(() => membership.ChangeRoleAsync(creation.UserId, workspace.Id, creation.UserId, WorkspaceRole.Developer, CancellationToken.None));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => membership.ChangeRoleAsync(Guid.NewGuid(), workspace.Id, creation.UserId, WorkspaceRole.ReadOnly, CancellationToken.None));
            Assert.Equal(WorkspaceRole.Owner, (await members.FindAsync(workspace.Id, creation.UserId, CancellationToken.None))?.Role);
            var sessionsRepository = new SessionRepository(db);
            var sessionService = new SessionService(sessionsRepository, unit, new TokenSecrets(), new TestIssuer(), TimeProvider.System);
            var credentials = await sessionService.CreateAsync(creation.UserId, CancellationToken.None);
            Assert.True(await sessionsRepository.IsActiveAsync(creation.UserId, credentials.SessionId, DateTimeOffset.UtcNow, CancellationToken.None));
            Assert.Null(await sessionService.RefreshAsync(credentials.RefreshToken, "incorrect-csrf", CancellationToken.None));
            var rotated = await sessionService.RefreshAsync(credentials.RefreshToken, credentials.CsrfToken, CancellationToken.None);
            Assert.NotNull(rotated);
            Assert.NotEqual(credentials.RefreshToken, rotated.RefreshToken);
            Assert.Null(await sessionService.RefreshAsync(credentials.RefreshToken, credentials.CsrfToken, CancellationToken.None));
            Assert.False(await sessionsRepository.IsActiveAsync(creation.UserId, credentials.SessionId, DateTimeOffset.UtcNow, CancellationToken.None));
            Assert.Null(await sessionService.RefreshAsync(rotated.RefreshToken, rotated.CsrfToken, CancellationToken.None));
            var concurrent = await sessionService.CreateAsync(creation.UserId, CancellationToken.None);
            async Task<SessionCredentials?> Compete()
            {
                await using var competingDb = Context(wb.ConnectionString);
                await competingDb.Database.OpenConnectionAsync();
                await competingDb.Database.ExecuteSqlInterpolatedAsync($"SELECT set_config('search_path', {schema}, false)");
                return await new SessionService(new SessionRepository(competingDb), new UnitOfWork(competingDb), new TokenSecrets(), new TestIssuer(), TimeProvider.System)
                    .RefreshAsync(concurrent.RefreshToken, concurrent.CsrfToken, CancellationToken.None);
            }
            var responses = await Task.WhenAll(Compete(), Compete());
            Assert.Single(responses, x => x is not null);
            Assert.False(await sessionsRepository.IsActiveAsync(creation.UserId, concurrent.SessionId, DateTimeOffset.UtcNow, CancellationToken.None));
            var logout = await sessionService.CreateAsync(creation.UserId, CancellationToken.None);
            await sessionService.RevokeAsync(creation.UserId, logout.SessionId, CancellationToken.None);
            Assert.False(await sessionsRepository.IsActiveAsync(creation.UserId, logout.SessionId, DateTimeOffset.UtcNow, CancellationToken.None));
            var reset = await sessionService.CreateAsync(creation.UserId, CancellationToken.None);
            var resetUser = await users.FindByIdAsync(creation.UserId.ToString()) ?? throw new InvalidOperationException();
            Assert.True((await users.UpdateSecurityStampAsync(resetUser)).Succeeded);
            await unit.SaveChangesAsync(CancellationToken.None);
            Assert.False(await sessionsRepository.IsActiveAsync(creation.UserId, reset.SessionId, DateTimeOffset.UtcNow, CancellationToken.None));
            Assert.Null(await sessionService.RefreshAsync(reset.RefreshToken, reset.CsrfToken, CancellationToken.None));
            var mfaUser = await users.FindByIdAsync(creation.UserId.ToString()) ?? throw new InvalidOperationException();
            var secureStore = (SecureUserStore)scope.ServiceProvider.GetRequiredService<IUserStore<AccountUser>>();
            var authenticatorKey = users.GenerateNewAuthenticatorKey();
            await unit.ExecuteAsync(token => secureStore.SetAuthenticatorKeyAsync(mfaUser, authenticatorKey, token), CancellationToken.None);
            var storedAuthenticator = await admin.UserTokens.AsNoTracking().SingleAsync(x => x.UserId == creation.UserId && x.Name == "AuthenticatorKey");
            Assert.StartsWith("DP1:", storedAuthenticator.Value);
            Assert.DoesNotContain(authenticatorKey, storedAuthenticator.Value);
            Assert.Equal(authenticatorKey, await secureStore.GetAuthenticatorKeyAsync(mfaUser, CancellationToken.None));
            var restoredStore = new SecureUserStore(db, DataProtectionProvider.Create(keyDirectory)) { AutoSaveChanges = false };
            Assert.Equal(authenticatorKey, await restoredStore.GetAuthenticatorKeyAsync(mfaUser, CancellationToken.None));
            var independentOtp = new Totp(Base32Encoding.ToBytes(authenticatorKey)).ComputeTotp();
            Assert.True(await users.VerifyTwoFactorTokenAsync(mfaUser, TokenOptions.DefaultAuthenticatorProvider, independentOtp));
            var recoveryCodes = new[] { new TokenSecrets().Generate(), new TokenSecrets().Generate() };
            await unit.ExecuteAsync(token => secureStore.ReplaceCodesAsync(mfaUser, recoveryCodes, token), CancellationToken.None);
            var storedRecovery = await admin.UserTokens.AsNoTracking().SingleAsync(x => x.UserId == creation.UserId && x.Name == "RecoveryCodes");
            Assert.DoesNotContain(recoveryCodes[0], storedRecovery.Value);
            bool redeemed = false;
            await unit.ExecuteAsync(async token => { redeemed = await secureStore.RedeemCodeAsync(mfaUser, recoveryCodes[0], token); }, CancellationToken.None);
            Assert.True(redeemed);
            await unit.ExecuteAsync(async token => { redeemed = await secureStore.RedeemCodeAsync(mfaUser, recoveryCodes[0], token); }, CancellationToken.None);
            Assert.False(redeemed);
            await unit.ExecuteAsync(async token => { redeemed = await secureStore.RedeemCodeAsync(mfaUser, recoveryCodes[1], token); }, CancellationToken.None);
            Assert.True(redeemed);
            Assert.Equal(0, await secureStore.CountCodesAsync(mfaUser, CancellationToken.None));
            var query = new WorkspaceQueries(rb.ConnectionString, schema);
            Assert.Equal(workspace.Name, (await query.FindAsync(workspace.Id, CancellationToken.None))?.Name);
            Assert.Null(await query.FindAsync(Guid.NewGuid(), CancellationToken.None));
            await using var readConnection = new NpgsqlConnection(rb.ConnectionString);
            await readConnection.OpenAsync();
            await using var writeAttempt = new NpgsqlCommand($"INSERT INTO {schema}.workspaces (id,name,time_zone,created_at) VALUES (gen_random_uuid(),'denied','UTC',now())", readConnection);
            var denied = await Assert.ThrowsAsync<PostgresException>(() => writeAttempt.ExecuteNonQueryAsync());
            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, denied.SqlState);
            await using var rawRead = new NpgsqlCommand($"SELECT * FROM {schema}.workspaces LIMIT 1", readConnection);
            denied = await Assert.ThrowsAsync<PostgresException>(() => rawRead.ExecuteReaderAsync());
            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, denied.SqlState);
        }
        finally
        {
            await Execute(owner, $"DROP SCHEMA {schema} CASCADE");
        }
    }
    private sealed class TestIssuer : IAccessTokenIssuer { public string Issue(Guid userId, Guid sessionId, DateTimeOffset now) => "local-test-access"; }
    private static string Required(string name) => Environment.GetEnvironmentVariable(name) ??
        throw new InvalidOperationException($"Configure {name}; this fixture requires real PostgreSQL and never silently passes.");
    private static NotifyHubDbContext Context(string connection) =>
        new(new DbContextOptionsBuilder<NotifyHubDbContext>().UseNpgsql(connection, options => options.MigrationsHistoryTable("__EFMigrationsHistory", new NpgsqlConnectionStringBuilder(connection).SearchPath)).Options);
    private static async Task Execute(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}











