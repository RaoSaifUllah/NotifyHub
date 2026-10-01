using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace NotifyHub.Infrastructure.Persistence;
public sealed class DesignTimeFactory : IDesignTimeDbContextFactory<NotifyHubDbContext>
{
    public NotifyHubDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<NotifyHubDbContext>().UseNpgsql(
            DatabaseConnection.Normalize(Environment.GetEnvironmentVariable("ConnectionStrings__Migration") ??
            "Host=localhost;Database=notifyhub_design_only;Username=design_only")).Options);
}

