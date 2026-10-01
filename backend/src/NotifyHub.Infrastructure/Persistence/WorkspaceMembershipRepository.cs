using Microsoft.EntityFrameworkCore;
using NotifyHub.Application.Workspaces;
using NotifyHub.Domain.Workspaces;
namespace NotifyHub.Infrastructure.Persistence;
public sealed class WorkspaceMembershipRepository(NotifyHubDbContext db) : IWorkspaceMembershipRepository
{
    public Task<WorkspaceMember?> FindAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken) =>
        db.Set<WorkspaceMember>().SingleOrDefaultAsync(x => x.WorkspaceId == workspaceId && x.UserId == userId, cancellationToken);
    public Task<int> OwnerCountAsync(Guid workspaceId, CancellationToken cancellationToken) =>
        db.Set<WorkspaceMember>().CountAsync(x => x.WorkspaceId == workspaceId && x.Role == WorkspaceRole.Owner, cancellationToken);
    public void Add(WorkspaceMember member) => db.Set<WorkspaceMember>().Add(member);
}
