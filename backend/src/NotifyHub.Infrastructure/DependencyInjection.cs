using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using NotifyHub.Infrastructure.Identity;
using NotifyHub.Application.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotifyHub.Application.Workspaces;
using NotifyHub.Infrastructure.Persistence;
namespace NotifyHub.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddDbContext<NotifyHubDbContext>(options => options.UseNpgsql(DatabaseConnection.Normalize(configuration.GetConnectionString("Write"))));
        services.AddDataProtection().SetApplicationName("NotifyHub").PersistKeysToFileSystem(
            new DirectoryInfo(configuration["Security:IdentityKeyDirectory"] ?? Path.Combine(AppContext.BaseDirectory, "secrets", "identity-keys")));
        services.AddIdentityCore<AccountUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 12;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireDigit = false;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        }).AddEntityFrameworkStores<NotifyHubDbContext>().AddTokenProvider<AuthenticatorTokenProvider<AccountUser>>(TokenOptions.DefaultAuthenticatorProvider);
        services.AddScoped<IPasswordHasher<AccountUser>, Argon2PasswordHasher>();
        services.AddScoped<IUserStore<AccountUser>>(provider =>
            new SecureUserStore(provider.GetRequiredService<NotifyHubDbContext>(), provider.GetRequiredService<IDataProtectionProvider>()) { AutoSaveChanges = false });
        services.AddSingleton<ITokenSecrets, TokenSecrets>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IAccountBootstrap, AccountBootstrap>();
        services.AddScoped<IWorkspaceMembershipRepository, WorkspaceMembershipRepository>();
        services.AddScoped<MembershipService>();
        services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IWorkspaceQueries>(_ => new WorkspaceQueries(DatabaseConnection.Normalize(configuration.GetConnectionString("Read"))));
        return services;
    }
}




