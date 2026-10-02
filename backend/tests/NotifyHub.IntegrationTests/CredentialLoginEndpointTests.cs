using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NotifyHub.Api.Operations;
using NotifyHub.Application.Identity;

namespace NotifyHub.IntegrationTests;

public sealed class CredentialLoginEndpointTests
{
    [Fact]
    public async Task PasswordFailureIsGenericAndEnrollmentReturnsNoSession()
    {
        var service = new ProbeLogin();
        await using var factory = Factory(service);
        using var client = Client(factory);
        using var failure = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "admin@example.test", password = "wrong" });
        Assert.Equal(HttpStatusCode.Unauthorized, failure.StatusCode);
        Assert.Equal("no-store", failure.Headers.CacheControl?.ToString());
        var problem = await failure.Content.ReadAsStringAsync();
        Assert.Contains("LOGIN_FAILED", problem);
        Assert.DoesNotContain("admin@example.test", problem);
        Assert.False(failure.Headers.Contains("Set-Cookie"));
        service.Enroll = true;
        using var enrollment = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "admin@example.test", password = "correct" });
        Assert.Equal(HttpStatusCode.OK, enrollment.StatusCode);
        using var json = JsonDocument.Parse(await enrollment.Content.ReadAsStringAsync());
        Assert.Equal("EnrollMfa", json.RootElement.GetProperty("status").GetString());
        Assert.False(json.RootElement.TryGetProperty("accessToken", out _));
        Assert.False(enrollment.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task CompletionRequiresCsrfAndKeepsRefreshSecretOutOfBody()
    {
        var service = new ProbeLogin();
        await using var factory = Factory(service);
        using var client = Client(factory);
        var payload = new { challengeToken = new string('C', 43), code = "123456" };
        using var noCsrf = await client.PostAsJsonAsync("/api/v1/auth/mfa/complete", payload);
        Assert.Equal(HttpStatusCode.Unauthorized, noCsrf.StatusCode);
        Assert.Equal(0, service.Completions);
        client.DefaultRequestHeaders.Add("X-CSRF-Token", new string('S', 43));
        using var completed = await client.PostAsJsonAsync("/api/v1/auth/mfa/complete", payload);
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        var json = await completed.Content.ReadAsStringAsync();
        Assert.DoesNotContain("private-refresh-secret", json);
        Assert.Contains("accessToken", json);
        var cookies = completed.Headers.GetValues("Set-Cookie").ToArray();
        Assert.Equal(2, cookies.Length);
        Assert.Contains(cookies, x => x.StartsWith("__Secure-notifyhub-refresh=", StringComparison.Ordinal) && x.Contains("httponly"));
        Assert.Contains(cookies, x => x.StartsWith("__Host-notifyhub-csrf=", StringComparison.Ordinal) && x.Contains("secure") && !x.Contains("httponly"));
        Assert.Equal(1, service.Completions);
    }

    [Fact]
    public async Task MissingOriginNeverEvaluatesPassword()
    {
        var service = new ProbeLogin();
        await using var factory = Factory(service);
        using var client = Client(factory);
        client.DefaultRequestHeaders.Remove("Origin");
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "admin@example.test", password = "secret" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, service.Starts);
    }

    private static WebApplicationFactory<Program> Factory(ProbeLogin service) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.AddSingleton(new BrowserSessionPolicy(["https://app.example.test"]));
            services.AddSingleton<ICredentialLogin>(service);
        }));
    private static HttpClient Client(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        client.DefaultRequestHeaders.Add("Origin", "https://app.example.test");
        return client;
    }
    private sealed class ProbeLogin : ICredentialLogin
    {
        public bool Enroll { get; set; }
        public int Starts { get; private set; }
        public int Completions { get; private set; }
        public Task<LoginStart?> BeginAsync(string email, string password, CancellationToken cancellationToken)
        {
            Starts++;
            return Task.FromResult<LoginStart?>(Enroll ?
                new LoginStart("EnrollMfa", new string('C', 43), new string('S', 43), new string('A', 32), null) : null);
        }
        public Task<LoginCompletion?> CompleteMfaAsync(string challengeToken, string csrfToken, string code, bool recovery, CancellationToken cancellationToken)
        {
            Completions++;
            Assert.Equal(new string('C', 43), challengeToken);
            Assert.Equal(new string('S', 43), csrfToken);
            Assert.Equal("123456", code);
            return Task.FromResult<LoginCompletion?>(new LoginCompletion(
                new SessionCredentials("test-access-token", "private-refresh-secret", new string('S', 43), Guid.NewGuid()), []));
        }
    }
}