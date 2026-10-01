using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using NotifyHub.Application.Identity;
using NotifyHub.Domain.Identity;
using NotifyHub.Infrastructure.Identity;

namespace NotifyHub.IntegrationTests;

public sealed class AccessAuthenticationTests
{
    [Theory]
    [InlineData(true, false, HttpStatusCode.OK)]
    [InlineData(false, false, HttpStatusCode.Unauthorized)]
    [InlineData(true, true, HttpStatusCode.Unauthorized)]
    public async Task SignedTokenRequiresCurrentSession(bool active, bool unavailable, HttpStatusCode expected)
    {
        using var rsa = RSA.Create(3072);
        var user = Guid.NewGuid();
        var session = Guid.NewGuid();
        var repository = new SessionProbe(user, session, active, unavailable);
        await using var factory = CreateFactory(rsa, repository);
        using var client = factory.CreateClient();
        var issuer = new JwtTokenIssuer(rsa.ExportPkcs8PrivateKeyPem(), "NotifyHub", "NotifyHub.Web");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", issuer.Issue(user, session, DateTimeOffset.UtcNow));
        var response = await client.GetAsync("/api/v1/auth/session");
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal(1, repository.Checks);
        if (expected == HttpStatusCode.OK) Assert.Contains(session.ToString(), await response.Content.ReadAsStringAsync());
        else Assert.DoesNotContain("Session validation", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("malformed")]
    [InlineData("wrong-issuer")]
    [InlineData("wrong-audience")]
    [InlineData("expired")]
    [InlineData("future")]
    [InlineData("wrong-signature")]
    public async Task InvalidAccessNeverQueriesSession(string scenario)
    {
        using var rsa = RSA.Create(3072);
        using var other = RSA.Create(3072);
        var user = Guid.NewGuid();
        var session = Guid.NewGuid();
        var repository = new SessionProbe(user, session, true, false);
        await using var factory = CreateFactory(rsa, repository);
        using var client = factory.CreateClient();
        var issuer = new JwtTokenIssuer((scenario == "wrong-signature" ? other : rsa).ExportPkcs8PrivateKeyPem(),
            scenario == "wrong-issuer" ? "untrusted" : "NotifyHub",
            scenario == "wrong-audience" ? "untrusted" : "NotifyHub.Web");
        var now = DateTimeOffset.UtcNow;
        if (scenario == "expired") now = now.AddMinutes(-10);
        if (scenario == "future") now = now.AddMinutes(10);
        if (scenario != "missing")
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                scenario == "malformed" ? "invalid" : issuer.Issue(user, session, now));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/session")).StatusCode);
        Assert.Equal(0, repository.Checks);
    }

    private static WebApplicationFactory<Program> CreateFactory(RSA rsa, SessionProbe repository) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<ISessionRepository>();
            services.AddSingleton<ISessionRepository>(repository);
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                options.TokenValidationParameters.IssuerSigningKey = new RsaSecurityKey(rsa.ExportParameters(false)));
        }));

    private sealed class SessionProbe(Guid userId, Guid sessionId, bool active, bool unavailable) : ISessionRepository
    {
        public int Checks { get; private set; }
        public Task<bool> IsActiveAsync(Guid user, Guid session, DateTimeOffset now, CancellationToken cancellationToken)
        {
            Checks++;
            if (unavailable) throw new InvalidOperationException("Storage unavailable.");
            return Task.FromResult(active && user == userId && session == sessionId);
        }
        public void Add(RefreshSession session, RefreshToken token) => throw new NotSupportedException();
        public void Add(RefreshToken token) => throw new NotSupportedException();
        public Task<(RefreshSession Session, RefreshToken Token)?> LockByTokenHashAsync(string hash, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<RefreshSession?> LockSessionAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<UserSecurity?> UserSecurityAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}