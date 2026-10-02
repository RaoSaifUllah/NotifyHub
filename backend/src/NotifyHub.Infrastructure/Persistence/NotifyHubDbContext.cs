using NotifyHub.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using NotifyHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using NotifyHub.Domain.Workspaces;
namespace NotifyHub.Infrastructure.Persistence;
public sealed class NotifyHubDbContext(DbContextOptions<NotifyHubDbContext> options) : IdentityDbContext<AccountUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<AccountUser>().HasIndex(x => x.NormalizedEmail).IsUnique();
        var member = modelBuilder.Entity<WorkspaceMember>();
        member.ToTable("workspace_members");
        member.HasKey(x => new { x.WorkspaceId, x.UserId });
        member.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
        member.Property(x => x.Version).IsRowVersion();
        member.HasOne<Workspace>().WithMany().HasForeignKey(x => x.WorkspaceId).OnDelete(DeleteBehavior.Restrict);
        member.HasOne<AccountUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        var challenge = modelBuilder.Entity<AuthenticationChallenge>();
        challenge.ToTable("authentication_challenges");
        challenge.HasKey(x => x.Id);
        challenge.Property(x => x.Version).IsRowVersion();
        challenge.Property(x => x.TokenHash).HasMaxLength(64);
        challenge.Property(x => x.CsrfHash).HasMaxLength(64);
        challenge.Property(x => x.SecurityStamp).HasMaxLength(256);
        challenge.Property(x => x.Purpose).HasConversion<string>().HasMaxLength(20);
        challenge.HasIndex(x => x.TokenHash).IsUnique();
        challenge.HasOne<AccountUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        var session = modelBuilder.Entity<RefreshSession>();
        session.ToTable("refresh_sessions");
        session.HasKey(x => x.Id);
        session.Property(x => x.Version).IsRowVersion();
        session.Property(x => x.SecurityStamp).HasMaxLength(256);
        session.Property(x => x.CsrfHash).HasMaxLength(64);
        session.HasOne<AccountUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        var refresh = modelBuilder.Entity<RefreshToken>();
        refresh.ToTable("refresh_tokens");
        refresh.HasKey(x => x.Id);
        refresh.Property(x => x.Hash).HasMaxLength(64);
        refresh.HasIndex(x => x.Hash).IsUnique();
        refresh.HasOne<RefreshSession>().WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Cascade);
        var workspace = modelBuilder.Entity<Workspace>();
        workspace.ToTable("workspaces");
        workspace.HasKey(x => x.Id);
        workspace.Property(x => x.Id).HasColumnName("id");
        workspace.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        workspace.Property(x => x.TimeZone).HasColumnName("time_zone").HasMaxLength(100).IsRequired();
        workspace.Property(x => x.CreatedAt).HasColumnName("created_at");
        workspace.Property(x => x.Version).IsRowVersion();
    }
}


