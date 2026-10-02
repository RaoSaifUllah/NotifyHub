using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NotifyHub.Api.Operations;
using NotifyHub.Application.Identity;
using NotifyHub.Application.Workspaces;
using NotifyHub.Domain.Identity;

namespace NotifyHub.IntegrationTests;

public sealed class BrowserSessionTests
{
    [Theory]
    [InlineData("/refresh", null, true)]
    [InlineData("/refresh", "null", true)]
    [InlineData("/refresh", "https://app.example.test.evil.test", true)]
    [InlineData("/refresh", "https://app.example.test/", true)]
    [InlineData("/refresh", "https://app.example.test", false)]
    [InlineData("/logout", null, true)]
    [InlineData("/logout", "null", true)]
    [InlineData("/logout", "https://app.example.test.evil.test", true)]
    [InlineData("/logout", "https://app.example.test/", true)]
    [InlineData("/logout", "https://app.example.test", false)]
    public async Task BrowserBoundaryRejectsUntrustedOrInsecureRequests(string path, string? origin, bool https)
    {
        var fixture = new SessionFixture();
        await using var factory = Factory(fixture);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri(https ? "https://localhost" : "http://localhost"), HandleCookies = false
        });
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth" + path);
        if (origin is not null) request.Headers.TryAddWithoutValidation("Origin", origin);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("ORIGIN_DENIED", body.RootElement.GetProperty("code").GetString());
        Assert.Equal(0, fixture.Repository.Locks);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task RefreshRotatesSecureCookieAndNeverReturnsRefreshSecretInJson()
    {
        var fixture = new SessionFixture();
        var credentials = await fixture.Service.CreateAsync(fixture.Repository.UserId, CancellationToken.None);
        await using var factory = Factory(fixture);
        using var client = Client(factory);
        using var request = Request("/refresh", credentials.RefreshToken, credentials.CsrfToken);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(credentials.RefreshToken, json);
        Assert.DoesNotContain("refreshToken", json);
        var cookie = response.Headers.GetValues("Set-Cookie").Single();
        Assert.Contains("__Secure-notifyhub-refresh=", cookie);
        Assert.Contains("path=/api/v1/auth", cookie);
        Assert.Contains("secure", cookie);
        Assert.Contains("httponly", cookie);
        Assert.Contains("samesite=strict", cookie);
        Assert.DoesNotContain("domain=", cookie);
        var nextRefresh = cookie.Split(';')[0].Split('=', 2)[1];
        Assert.NotEqual(credentials.RefreshToken, nextRefresh);
        Assert.DoesNotContain(nextRefresh, json);
        using var replay = Request("/refresh", credentials.RefreshToken, credentials.CsrfToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(replay)).StatusCode);
        Assert.NotNull(fixture.Repository.Session!.RevokedAt);
    }

    [Fact]
    public async Task LogoutRequiresBoundCsrfBeforeRevoking()
    {
        var fixture = new SessionFixture();
        var credentials = await fixture.Service.CreateAsync(fixture.Repository.UserId, CancellationToken.None);
        await using var factory = Factory(fixture);
        using var client = Client(factory);
        using var wrong = Request("/logout", credentials.RefreshToken, "wrong");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(wrong)).StatusCode);
        Assert.Null(fixture.Repository.Session!.RevokedAt);
        using var request = Request("/logout", credentials.RefreshToken, credentials.CsrfToken);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.NotNull(fixture.Repository.Session.RevokedAt);
        Assert.Contains("expires=", response.Headers.GetValues("Set-Cookie").Single());
        using var afterLogout = Request("/refresh", credentials.RefreshToken, credentials.CsrfToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(afterLogout)).StatusCode);
    }

    [Fact]
    public async Task AuthTransportIsRateLimited()
    {
        var fixture = new SessionFixture();
        await using var factory = Factory(fixture);
        using var client = Client(factory);
        for (var i = 0; i < 10; i++)
        {
            using var request = Request("/refresh", "unknown", "unknown");
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(request)).StatusCode);
        }
        using var blocked = Request("/refresh", "unknown", "unknown");
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.SendAsync(blocked)).StatusCode);
    }

    private static WebApplicationFactory<Program> Factory(SessionFixture fixture) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.AddSingleton(new BrowserSessionPolicy(["https://app.example.test"]));
            services.AddSingleton(fixture.Service);
        }));
    private static HttpClient Client(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
    private static HttpRequestMessage Request(string path, string refresh, string csrf)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth" + path);
        request.Headers.TryAddWithoutValidation("Origin", "https://app.example.test");
        request.Headers.TryAddWithoutValidation("Cookie", "__Secure-notifyhub-refresh=" + refresh);
        request.Headers.TryAddWithoutValidation("X-CSRF-Token", csrf);
        return request;
    }
    private sealed class SessionFixture
    {
        public MemorySessions Repository { get; } = new();
        public SessionService Service { get; }
        public SessionFixture() => Service = new SessionService(Repository, new MemoryUnit(),
            new NotifyHub.Infrastructure.Identity.TokenSecrets(), new TestIssuer(), TimeProvider.System);
    }
    private sealed class TestIssuer : IAccessTokenIssuer
    {
        public string Issue(Guid userId, Guid sessionId, DateTimeOffset now) => "test-access";
    }
    private sealed class MemoryUnit : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(0);
        public Task ExecuteAsync(Func<CancellationToken, Task> command, CancellationToken cancellationToken) => command(cancellationToken);
    }
    private sealed class MemorySessions : ISessionRepository
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public RefreshSession? Session { get; private set; }
        public int Locks { get; private set; }
        private readonly Dictionary<string, RefreshToken> tokens = [];
        public void Add(RefreshSession session, RefreshToken token) { Session = session; Add(token); }
        public void Add(RefreshToken token) => tokens.Add(token.Hash, token);
        public Task<(RefreshSession Session, RefreshToken Token)?> LockByTokenHashAsync(string hash, CancellationToken cancellationToken)
        {
            Locks++;
            return Task.FromResult<(RefreshSession Session, RefreshToken Token)?>(
                Session is not null && tokens.TryGetValue(hash, out var token) ? (Session, token) : null);
        }
        public Task<RefreshSession?> LockSessionAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Session?.Id == id ? Session : null);
        public Task<UserSecurity?> UserSecurityAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<UserSecurity?>(id == UserId ? new UserSecurity(UserId, "test-stamp", false) : null);
        public Task<bool> IsActiveAsync(Guid user, Guid session, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.FromResult(user == UserId && Session?.Id == session && Session.IsActive(now));
    }
}