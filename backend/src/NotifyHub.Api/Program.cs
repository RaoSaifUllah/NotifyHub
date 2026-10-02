using NotifyHub.Api.Operations;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using NotifyHub.Infrastructure;
using NotifyHub.Infrastructure.Persistence;
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false).AddEnvironmentVariables();
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 32 * 1024);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.AddAccessAuthentication(builder.Configuration);
builder.Services.AddBrowserSessions(builder.Configuration);
builder.Services.AddExceptionHandler<SafeExceptionHandler>();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    context.ProblemDetails.Extensions["correlationId"] = Activity.Current?.TraceId.ToString() ?? context.HttpContext.TraceIdentifier;
    context.ProblemDetails.Extensions.TryAdd("code", context.ProblemDetails.Status == 503 ? "DEPENDENCY_UNAVAILABLE" : "REQUEST_FAILED");
});
var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["X-Correlation-ID"] = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
    await next(context);
});
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapBrowserSessions();
app.MapGet("/api/v1/auth/session", (HttpContext context) =>
{
    context.Response.Headers.CacheControl = "no-store";
    return Results.Ok(new { userId = context.User.FindFirst("sub")!.Value, sessionId = context.User.FindFirst("sid")!.Value });
}).RequireAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", version = "0.1.0" }));
app.MapGet("/ready", async (NotifyHubDbContext db, CancellationToken cancellationToken) =>
{
    try
    {
        if (await db.Database.CanConnectAsync(cancellationToken) && !(await db.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
            return Results.Ok(new { status = "ready" });
    }
    catch (Exception) { }
    return Results.Problem(statusCode: 503, title: "Database is unavailable or requires migration.");
});
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.Run();
public partial class Program { }


