using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NotifyHub.Application.Identity;
using NotifyHub.Infrastructure.Identity;

namespace NotifyHub.Api.Operations;

public static class AccessAuthentication
{
    public static IServiceCollection AddAccessAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // An unconfigured installation exposes health only and cannot authenticate.
        var path = configuration["Security:Jwt:PrivateKeyPath"];
        RsaSecurityKey? key = null;
        if (!string.IsNullOrWhiteSpace(path))
        {
            try
            {
                var pem = File.ReadAllText(path);
                using var rsa = RSA.Create();
                rsa.ImportFromPem(pem);
                if (rsa.KeySize < 3072) throw new InvalidOperationException();
                key = new RsaSecurityKey(rsa.ExportParameters(false))
                {
                    KeyId = Convert.ToHexString(SHA256.HashData(rsa.ExportSubjectPublicKeyInfo()))[..16]
                };
                services.AddSingleton<IAccessTokenIssuer>(new JwtTokenIssuer(pem,
                    configuration["Security:Jwt:Issuer"] ?? "NotifyHub",
                    configuration["Security:Jwt:Audience"] ?? "NotifyHub.Web"));
                services.AddScoped<SessionService>();
                services.AddScoped<ICredentialLogin, CredentialLogin>();
            }
            catch (Exception)
            {
                throw new InvalidOperationException("JWT signing key configuration is invalid.");
            }
        }
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.IncludeErrorDetails = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = configuration["Security:Jwt:Issuer"] ?? "NotifyHub",
                ValidateAudience = true,
                ValidAudience = configuration["Security:Jwt:Audience"] ?? "NotifyHub.Web",
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                RequireSignedTokens = true,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                ClockSkew = TimeSpan.FromSeconds(15),
                ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                ValidTypes = ["at+jwt"],
                NameClaimType = "sub"
            };
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var principal = context.Principal;
                    if (!Guid.TryParse(principal?.FindFirstValue("sub"), out var userId) ||
                        !Guid.TryParse(principal?.FindFirstValue("sid"), out var sessionId))
                    {
                        context.Fail("Invalid session.");
                        return;
                    }
                    try
                    {
                        var repository = context.HttpContext.RequestServices.GetRequiredService<ISessionRepository>();
                        var clock = context.HttpContext.RequestServices.GetRequiredService<TimeProvider>();
                        if (!await repository.IsActiveAsync(userId, sessionId, clock.GetUtcNow(), context.HttpContext.RequestAborted))
                            context.Fail("Inactive session.");
                    }
                    catch (Exception)
                    {
                        // Storage failures never turn a signed token into an authenticated request.
                        context.Fail("Session validation unavailable.");
                    }
                }
            };
        });
        services.AddAuthorization();
        return services;
    }
}