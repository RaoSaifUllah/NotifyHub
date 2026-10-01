using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NotifyHub.Infrastructure.Persistence;
namespace NotifyHub.IntegrationTests;
public class ApiFoundationTests
{
    private static WebApplicationFactory<Program> Factory() => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<NotifyHubDbContext>>();
            services.RemoveAll<NotifyHubDbContext>();
            services.AddDbContext<NotifyHubDbContext>(options => options.UseNpgsql("Host=127.0.0.1;Port=1;Database=unavailable;Username=test;Timeout=1"));
        });
    });
    [Fact]
    public async Task HealthIsIndependentOfUnavailableDatabase()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.True(response.Headers.Contains("X-Correlation-ID"));
        var ready = await client.GetAsync("/ready");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
        using var problem = JsonDocument.Parse(await ready.Content.ReadAsStringAsync());
        Assert.Equal("DEPENDENCY_UNAVAILABLE", problem.RootElement.GetProperty("code").GetString());
        Assert.True(problem.RootElement.TryGetProperty("correlationId", out _));
    }
    [Fact]
    public async Task MissingRoutesReturnProblemDetails()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/missing");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(404, problem.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("REQUEST_FAILED", problem.RootElement.GetProperty("code").GetString());
    }
    [Fact]
    public async Task DevelopmentContractDocumentsHealthAndReadiness()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        using var contract = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        Assert.StartsWith("3.", contract.RootElement.GetProperty("openapi").GetString());
        Assert.True(contract.RootElement.GetProperty("paths").TryGetProperty("/health", out _));
        Assert.True(contract.RootElement.GetProperty("paths").TryGetProperty("/ready", out _));
    }
}
