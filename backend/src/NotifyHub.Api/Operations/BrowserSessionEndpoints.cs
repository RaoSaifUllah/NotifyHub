using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using NotifyHub.Application.Identity;

namespace NotifyHub.Api.Operations;

public sealed class BrowserSessionPolicy(IEnumerable<string> allowedOrigins)
{
    private readonly HashSet<string> origins = new(allowedOrigins, StringComparer.Ordinal);

    public bool Allows(HttpRequest request)
    {
        var values = request.Headers.Origin;
        return request.IsHttps && values.Count == 1 && values[0] is { Length: <= 256 } origin &&
            origin != "null" && origins.Contains(origin);
    }
}

public static class BrowserSessionEndpoints
{
    private const string RefreshCookie = "__Secure-notifyhub-refresh";
    private const string CookiePath = "/api/v1/auth";

    public static IServiceCollection AddBrowserSessions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(new BrowserSessionPolicy(
            configuration.GetSection("Security:BrowserOrigins").Get<string[]>() ?? []));
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0,
                    AutoReplenishment = true
                }));
        });
        return services;
    }

    public static void MapBrowserSessions(this WebApplication app)
    {
        var group = app.MapGroup(CookiePath).RequireRateLimiting("auth");
        group.MapPost("/refresh", async (HttpContext context, BrowserSessionPolicy policy, CancellationToken cancellationToken) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            if (!policy.Allows(context.Request)) return DeniedOrigin();
            var service = context.RequestServices.GetService<SessionService>();
            if (service is null) return Results.Problem(statusCode: 503, title: "Authentication is not configured.");
            if (!ReadSecrets(context.Request, out var refresh, out var csrf)) return Results.Unauthorized();
            var credentials = await service.RefreshAsync(refresh, csrf, cancellationToken);
            if (credentials is null)
            {
                ClearCookie(context.Response);
                return Results.Unauthorized();
            }
            WriteSessionCookies(context.Response, credentials);
            return Results.Ok(new { credentials.AccessToken, credentials.CsrfToken, credentials.SessionId, expiresIn = 300 });
        });
        group.MapPost("/logout", async (HttpContext context, BrowserSessionPolicy policy, CancellationToken cancellationToken) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            if (!policy.Allows(context.Request)) return DeniedOrigin();
            var service = context.RequestServices.GetService<SessionService>();
            if (service is null) return Results.Problem(statusCode: 503, title: "Authentication is not configured.");
            if (!ReadSecrets(context.Request, out var refresh, out var csrf)) return Results.Unauthorized();
            if (!await service.RevokeFromRefreshAsync(refresh, csrf, cancellationToken)) return Results.Unauthorized();
            ClearCookie(context.Response);
            return Results.NoContent();
        });
    }

    public static void WriteCookie(HttpResponse response, string refreshToken) =>
        response.Cookies.Append(RefreshCookie, refreshToken, CookieOptions());

    public static void WriteSessionCookies(HttpResponse response, SessionCredentials credentials)
    {
        WriteCookie(response, credentials.RefreshToken);
        // Only the CSRF value is JS-readable. JWT stays in memory and refresh stays HttpOnly.
        response.Cookies.Append("__Host-notifyhub-csrf", credentials.CsrfToken, CsrfCookieOptions());
    }
    private static CookieOptions CsrfCookieOptions() => new()
    {
        Secure = true, HttpOnly = false, SameSite = SameSiteMode.Strict, Path = "/", IsEssential = true
    };
    private static void ClearCookie(HttpResponse response)
    {
        response.Cookies.Delete(RefreshCookie, CookieOptions());
        response.Cookies.Delete("__Host-notifyhub-csrf", CsrfCookieOptions());
    }

    private static CookieOptions CookieOptions() => new()
    {
        HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Path = CookiePath,
        IsEssential = true
        // Session cookie: no persistent browser expiry and no Domain attribute.
    };

    private static bool ReadSecrets(HttpRequest request, out string refresh, out string csrf)
    {
        refresh = request.Cookies[RefreshCookie] ?? "";
        var headers = request.Headers["X-CSRF-Token"];
        csrf = headers.Count == 1 ? headers[0] ?? "" : "";
        return refresh.Length is > 0 and <= 512 && csrf.Length is > 0 and <= 512;
    }

    private static IResult DeniedOrigin() => Results.Problem(statusCode: 403,
        title: "Browser origin is not allowed.", extensions: new Dictionary<string, object?> { ["code"] = "ORIGIN_DENIED" });
}