using NotifyHub.Application.Identity;

namespace NotifyHub.Api.Operations;

public sealed record LoginRequest(string Email, string Password);
public sealed record MfaRequest(string ChallengeToken, string Code, bool Recovery = false);

public static class CredentialLoginEndpoints
{
    private static readonly SemaphoreSlim PasswordSlots = new(2, 2);
    public static void MapCredentialLogin(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/auth").RequireRateLimiting("auth");
        group.MapPost("/login", async (LoginRequest request, HttpContext context, BrowserSessionPolicy policy,
            CancellationToken cancellationToken) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            if (!policy.Allows(context.Request)) return DeniedOrigin();
            var service = context.RequestServices.GetService<ICredentialLogin>();
            if (service is null) return Results.Problem(statusCode: 503, title: "Authentication is not configured.");
            if (!await PasswordSlots.WaitAsync(0, cancellationToken))
                return Results.Problem(statusCode: 429, title: "Try again shortly.");
            try
            {
                if (request.Email is null || request.Password is null) return InvalidCredentials();
                var result = await service.BeginAsync(request.Email, request.Password, cancellationToken);
                if (result is null) return InvalidCredentials();
                if (result.Credentials is { } credentials)
                {
                    BrowserSessionEndpoints.WriteSessionCookies(context.Response, credentials);
                    return Results.Ok(new { status = "Authenticated", credentials.AccessToken, credentials.CsrfToken,
                        credentials.SessionId, expiresIn = 300, recoveryCodes = Array.Empty<string>() });
                }
                return Results.Ok(new { result.Status, result.ChallengeToken, result.CsrfToken, result.AuthenticatorKey });
            }
            finally { PasswordSlots.Release(); }
        });
        group.MapPost("/mfa/complete", async (MfaRequest request, HttpContext context,
            BrowserSessionPolicy policy, CancellationToken cancellationToken) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            if (!policy.Allows(context.Request)) return DeniedOrigin();
            var service = context.RequestServices.GetService<ICredentialLogin>();
            if (service is null) return Results.Problem(statusCode: 503, title: "Authentication is not configured.");
            var csrf = context.Request.Headers["X-CSRF-Token"];
            if (csrf.Count != 1 || request.ChallengeToken is null || request.Code is null) return InvalidCredentials();
            var result = await service.CompleteMfaAsync(request.ChallengeToken, csrf[0] ?? "",
                request.Code, request.Recovery, cancellationToken);
            if (result is null) return InvalidCredentials();
            BrowserSessionEndpoints.WriteSessionCookies(context.Response, result.Credentials);
            return Results.Ok(new { status = "Authenticated", result.Credentials.AccessToken, result.Credentials.CsrfToken,
                result.Credentials.SessionId, expiresIn = 300, result.RecoveryCodes });
        });
    }
    private static IResult InvalidCredentials() => Results.Problem(statusCode: 401,
        title: "Sign-in could not be completed.", extensions: new Dictionary<string, object?> { ["code"] = "LOGIN_FAILED" });
    private static IResult DeniedOrigin() => Results.Problem(statusCode: 403,
        title: "Browser origin is not allowed.", extensions: new Dictionary<string, object?> { ["code"] = "ORIGIN_DENIED" });
}