using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NotifyHub.Api.Operations;
namespace NotifyHub.IntegrationTests;
public class ErrorRedactionTests
{
    [Theory]
    [InlineData(400)]
    [InlineData(403)]
    [InlineData(404)]
    [InlineData(500)]
    public async Task ErrorsNeverExposeExceptionCredentials(int status)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProblemDetails();
        using var provider = services.BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = provider, TraceIdentifier = "test-correlation" };
        context.Response.Body = new MemoryStream();
        Exception exception = status switch {
            400 => new ArgumentException("password=never-expose"),
            403 => new UnauthorizedAccessException("password=never-expose"),
            404 => new KeyNotFoundException("password=never-expose"),
            _ => new InvalidOperationException("password=never-expose")
        };
        var handler = new SafeExceptionHandler(provider.GetRequiredService<IProblemDetailsService>());
        Assert.True(await handler.TryHandleAsync(context, exception, CancellationToken.None));
        Assert.Equal(status, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.DoesNotContain("never-expose", body);
        using var problem = JsonDocument.Parse(body);
        Assert.Equal("test-correlation", problem.RootElement.GetProperty("correlationId").GetString());
    }
}
