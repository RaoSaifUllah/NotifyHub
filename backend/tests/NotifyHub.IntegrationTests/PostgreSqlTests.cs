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
            foreach (var table in new[] { "AspNetUsers", "AspNetRoles", "AspNetUserClaims", "AspNetUserLogins", "AspNetUserRoles", "AspNetUserTokens", "AspNetRoleClaims", "workspace_members", "refresh_sessions", "refresh_tokens", "authentication_challenges" })
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
            var bootstrap = new AccountBootstrap(db, users, unit, TimeProvider.System);
            await Assert.ThrowsAsync<ArgumentException>(() => bootstrap.CreateAdministratorAsync("invalid-email",
                "Local-Only-Test-Password1!", CancellationToken.None));
            Assert.False(await admin.Users.AnyAsync());
            async Task<Guid?> CompeteBootstrap(string email)
            {
                await using var competingDb = Context(wb.ConnectionString);
                await competingDb.Database.OpenConnectionAsync();
                await competingDb.Database.ExecuteSqlInterpolatedAsync($"SELECT set_config('search_path', {schema}, false)");
                var bootstrapServices = new ServiceCollection();
                bootstrapServices.AddLogging();
                bootstrapServices.AddScoped(_ => competingDb);
                bootstrapServices.AddIdentityCore<AccountUser>().AddEntityFrameworkStores<NotifyHubDbContext>();
                bootstrapServices.AddScoped<IUserStore<AccountUser>>(_ =>
                    new SecureUserStore(competingDb, protection) { AutoSaveChanges = false });
                bootstrapServices.AddScoped<IPasswordHasher<AccountUser>, Argon2PasswordHasher>();
                await using var bootstrapProvider = bootstrapServices.BuildServiceProvider();
                await using var bootstrapScope = bootstrapProvider.CreateAsyncScope();
                var competingBootstrap = new AccountBootstrap(competingDb,
                    bootstrapScope.ServiceProvider.GetRequiredService<UserManager<AccountUser>>(),
                    new UnitOfWork(competingDb), TimeProvider.System);
                try
                {
                    return await competingBootstrap.CreateAdministratorAsync(email,
                        "Local-Only-Test-Password1!", CancellationToken.None);
                }
                catch (InvalidOperationException) { return null; }
            }
            var bootstrapResults = await Task.WhenAll(
                CompeteBootstrap("first-admin@notifyhub.example"), CompeteBootstrap("second-admin@notifyhub.example"));
            Assert.Single(bootstrapResults, x => x is not null);
            var administratorId = bootstrapResults.Single(x => x is not null)!.Value;
            var administrator = await admin.Users.AsNoTracking().SingleAsync(x => x.Id == administratorId);
            Assert.True(administrator.IsSystemAdministrator);
            Assert.True(administrator.EmailConfirmed);
            Assert.True(administrator.LockoutEnabled);
            Assert.False(administrator.TwoFactorEnabled);
            Assert.StartsWith("$argon2id$", administrator.PasswordHash);
            await Assert.ThrowsAsync<InvalidOperationException>(() => bootstrap.CreateAdministratorAsync(
                "second-admin@notifyhub.example", "Local-Only-Test-Password1!", CancellationToken.None));
            Assert.Equal(1, await admin.Users.CountAsync());
            var loginStore = (SecureUserStore)scope.ServiceProvider.GetRequiredService<IUserStore<AccountUser>>();
            var credentialLogin = new CredentialLogin(db, users, loginStore, new Argon2PasswordHasher(),
                unit, new TokenSecrets(), new TestIssuer(), TimeProvider.System);
            Assert.Null(await credentialLogin.BeginAsync("missing@notifyhub.example", "incorrect-password", CancellationToken.None));
            var administratorEmail = administrator.Email!;
            Assert.Null(await credentialLogin.BeginAsync(administratorEmail, "incorrect-password", CancellationToken.None));
            var enrollment1 = await credentialLogin.BeginAsync(administratorEmail, "Local-Only-Test-Password1!", CancellationToken.None);
            Assert.NotNull(enrollment1);
            Assert.Equal("EnrollMfa", enrollment1.Status);
            Assert.Null(enrollment1.Credentials);
            Assert.NotNull(enrollment1.AuthenticatorKey);
            var enrollment = await credentialLogin.BeginAsync(administratorEmail, "Local-Only-Test-Password1!", CancellationToken.None);
            Assert.NotNull(enrollment);
            Assert.Null(await credentialLogin.CompleteMfaAsync(enrollment1.ChallengeToken!, enrollment1.CsrfToken!,
                "bad-code", false, CancellationToken.None));
            Assert.Null(await credentialLogin.CompleteMfaAsync(enrollment.ChallengeToken!, "wrong-csrf",
                "bad-code", false, CancellationToken.None));
            Assert.Null(await credentialLogin.CompleteMfaAsync(enrollment.ChallengeToken!, enrollment.CsrfToken!,
                "bad-code", false, CancellationToken.None));
            var enrollmentOtp = new Totp(Base32Encoding.ToBytes(enrollment.AuthenticatorKey!)).ComputeTotp();
            var enrolled = await credentialLogin.CompleteMfaAsync(enrollment.ChallengeToken!, enrollment.CsrfToken!,
                enrollmentOtp, false, CancellationToken.None);
            Assert.NotNull(enrolled);
            Assert.Equal(10, enrolled.RecoveryCodes.Length);
            Assert.All(enrolled.RecoveryCodes, value => Assert.Equal(43, value.Length));
            var enrolledSession = await admin.Set<RefreshSession>().AsNoTracking().SingleAsync(x => x.Id == enrolled.Credentials.SessionId);
            Assert.NotNull(enrolledSession.MfaVerifiedAt);
            var administratorAfterEnrollment = await admin.Users.AsNoTracking().SingleAsync(x => x.Id == administratorId);
            Assert.True(administratorAfterEnrollment.TwoFactorEnabled);
            Assert.NotNull(administratorAfterEnrollment.LastTotpStep);
            var challengeRow = await admin.Set<AuthenticationChallenge>().AsNoTracking()
                .SingleAsync(x => x.TokenHash == new TokenSecrets().Hash(enrollment.ChallengeToken!));
            Assert.NotNull(challengeRow.ConsumedAt);
            Assert.NotEqual(enrollment.ChallengeToken, challengeRow.TokenHash);
            var enrolledRecovery = await admin.UserTokens.AsNoTracking().SingleAsync(x => x.UserId == administratorId && x.Name == "RecoveryCodes");
            Assert.DoesNotContain(enrolled.RecoveryCodes[0], enrolledRecovery.Value);
            Assert.Null(await credentialLogin.CompleteMfaAsync(enrollment.ChallengeToken!, enrollment.CsrfToken!,
                enrollmentOtp, false, CancellationToken.None));
            var verification = await credentialLogin.BeginAsync(administratorEmail, "Local-Only-Test-Password1!", CancellationToken.None);
            Assert.NotNull(verification);
            Assert.Equal("VerifyMfa", verification.Status);
            Assert.Null(verification.AuthenticatorKey);
            // Even a valid code is rejected when its time step already authenticated enrollment.
            Assert.Null(await credentialLogin.CompleteMfaAsync(verification.ChallengeToken!, verification.CsrfToken!,
                enrollmentOtp, false, CancellationToken.None));
            var recovered = await credentialLogin.CompleteMfaAsync(verification.ChallengeToken!, verification.CsrfToken!,
                enrolled.RecoveryCodes[0], true, CancellationToken.None);
            Assert.NotNull(recovered);
            Assert.Empty(recovered.RecoveryCodes);
            Assert.False(await new SessionRepository(db).IsActiveAsync(administratorId, enrolled.Credentials.SessionId,
                DateTimeOffset.UtcNow, CancellationToken.None));
            var afterRecovery = await credentialLogin.BeginAsync(administratorEmail, "Local-Only-Test-Password1!", CancellationToken.None);
            Assert.NotNull(afterRecovery);
            Assert.Null(await credentialLogin.CompleteMfaAsync(afterRecovery.ChallengeToken!, afterRecovery.CsrfToken!,
                enrolled.RecoveryCodes[0], true, CancellationToken.None));
            Assert.NotNull(await credentialLogin.CompleteMfaAsync(afterRecovery.ChallengeToken!, afterRecovery.CsrfToken!,
                enrolled.RecoveryCodes[1], true, CancellationToken.None));
            var simultaneousMfa = await credentialLogin.BeginAsync(administratorEmail, "Local-Only-Test-Password1!", CancellationToken.None);
            Assert.NotNull(simultaneousMfa);
            async Task<LoginCompletion?> CompleteSameMfaChallenge()
            {
                await using var competingDb = Context(wb.ConnectionString);
                await competingDb.Database.OpenConnectionAsync();
                await competingDb.Database.ExecuteSqlInterpolatedAsync($"SELECT set_config('search_path', {schema}, false)");
                var competingServices = new ServiceCollection();
                competingServices.AddLogging();
                competingServices.AddScoped(_ => competingDb);
                competingServices.AddIdentityCore<AccountUser>().AddEntityFrameworkStores<NotifyHubDbContext>();
                competingServices.AddScoped<IUserStore<AccountUser>>(_ => new SecureUserStore(competingDb, protection) { AutoSaveChanges = false });
                await using var competingProvider = competingServices.BuildServiceProvider();
                await using var competingScope = competingProvider.CreateAsyncScope();
                var competingUsers = competingScope.ServiceProvider.GetRequiredService<UserManager<AccountUser>>();
                var competingStore = competingScope.ServiceProvider.GetRequiredService<IUserStore<AccountUser>>();
                return await new CredentialLogin(competingDb, competingUsers, competingStore, new Argon2PasswordHasher(),
                    new UnitOfWork(competingDb), new TokenSecrets(), new TestIssuer(), TimeProvider.System)
                    .CompleteMfaAsync(simultaneousMfa.ChallengeToken!, simultaneousMfa.CsrfToken!, enrolled.RecoveryCodes[2], true, CancellationToken.None);
            }
            var mfaRaces = await Task.WhenAll(CompleteSameMfaChallenge(), CompleteSameMfaChallenge());
            Assert.Single(mfaRaces, x => x is not null);
            db.ChangeTracker.Clear();
            for (var attempt = 0; attempt < 3; attempt++)
            {
                var rejectedMfa = await credentialLogin.BeginAsync(administratorEmail, "Local-Only-Test-Password1!", CancellationToken.None);
                Assert.NotNull(rejectedMfa);
                Assert.Null(await credentialLogin.CompleteMfaAsync(rejectedMfa.ChallengeToken!, rejectedMfa.CsrfToken!,
                    "bad-code", false, CancellationToken.None));
            }
            Assert.Equal(3, (await admin.Users.AsNoTracking().SingleAsync(x => x.Id == administratorId)).AccessFailedCount);
            for (var i = 0; i < 2; i++)
                Assert.Null(await credentialLogin.BeginAsync(administratorEmail, "incorrect-password", CancellationToken.None));
            Assert.Null(await credentialLogin.BeginAsync(administratorEmail, "Local-Only-Test-Password1!", CancellationToken.None));
            Assert.True((await admin.Users.AsNoTracking().SingleAsync(x => x.Id == administratorId)).LockoutEnd > DateTimeOffset.UtcNow);
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
            Assert.Null(await credentialLogin.BeginAsync("owner@notifyhub.example", "Local-Only-Test-Password1!", CancellationToken.None));
            var confirmedMember = await users.FindByIdAsync(creation.UserId.ToString()) ?? throw new InvalidOperationException();
            confirmedMember.EmailConfirmed = true;
            await unit.ExecuteAsync(async token => Assert.True((await users.UpdateAsync(confirmedMember)).Succeeded), CancellationToken.None);
            var memberLogin = await credentialLogin.BeginAsync("owner@notifyhub.example", "Local-Only-Test-Password1!", CancellationToken.None);
            Assert.NotNull(memberLogin?.Credentials);
            Assert.Equal("Authenticated", memberLogin.Status);
            Assert.Null((await admin.Set<RefreshSession>().AsNoTracking().SingleAsync(x => x.Id == memberLogin.Credentials!.SessionId)).MfaVerifiedAt);
            confirmedMember.Disabled = true;
            await unit.ExecuteAsync(async token => Assert.True((await users.UpdateSecurityStampAsync(confirmedMember)).Succeeded), CancellationToken.None);
            Assert.Null(await credentialLogin.BeginAsync("owner@notifyhub.example", "Local-Only-Test-Password1!", CancellationToken.None));
            Assert.False(await new SessionRepository(db).IsActiveAsync(creation.UserId, memberLogin.Credentials!.SessionId, DateTimeOffset.UtcNow, CancellationToken.None));
            confirmedMember.Disabled = false;
            await unit.ExecuteAsync(async token => Assert.True((await users.UpdateAsync(confirmedMember)).Succeeded), CancellationToken.None);
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
            var browserLogout = await sessionService.CreateAsync(creation.UserId, CancellationToken.None);
            Assert.False(await sessionService.RevokeFromRefreshAsync(browserLogout.RefreshToken, "wrong-csrf", CancellationToken.None));
            Assert.True(await sessionsRepository.IsActiveAsync(creation.UserId, browserLogout.SessionId, DateTimeOffset.UtcNow, CancellationToken.None));
            Assert.True(await sessionService.RevokeFromRefreshAsync(browserLogout.RefreshToken, browserLogout.CsrfToken, CancellationToken.None));
            Assert.False(await sessionsRepository.IsActiveAsync(creation.UserId, browserLogout.SessionId, DateTimeOffset.UtcNow, CancellationToken.None));
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
            foreach (var secretTable in new[] { "AspNetUsers", "AspNetUserTokens", "refresh_sessions", "refresh_tokens", "authentication_challenges" })
            {
                await using var secretRead = new NpgsqlCommand($"SELECT 1 FROM {schema}.{quote.QuoteIdentifier(secretTable)} LIMIT 1", readConnection);
                var secretDenied = await Assert.ThrowsAsync<PostgresException>(() => secretRead.ExecuteReaderAsync());
                Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, secretDenied.SqlState);
            }
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











