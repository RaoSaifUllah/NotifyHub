using NotifyHub.Domain.Workspaces;
namespace NotifyHub.Application.Workspaces;
public interface IWorkspaceMembershipRepository
{
    Task<WorkspaceMember?> FindAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken);
    Task<int> OwnerCountAsync(Guid workspaceId, CancellationToken cancellationToken);
    void Add(WorkspaceMember member);
}
public sealed class MembershipService(IWorkspaceRepository workspaces, IWorkspaceMembershipRepository members, IUnitOfWork unit)
{
    public Task ChangeRoleAsync(Guid actorId, Guid workspaceId, Guid targetId, WorkspaceRole role, CancellationToken cancellationToken) =>
        unit.ExecuteAsync(async token =>
        {
            // All membership mutations must lock the same workspace before checking owners.
            if (await workspaces.FindForUpdateAsync(workspaceId, token) is null) throw new KeyNotFoundException();
            var actor = await members.FindAsync(workspaceId, actorId, token);
            if (actor is null || !WorkspacePermissions.Allows(actor.Role, WorkspacePermission.ManageMembers))
                throw new UnauthorizedAccessException();
            var target = await members.FindAsync(workspaceId, targetId, token) ?? throw new KeyNotFoundException();
            target.ChangeRole(role, await members.OwnerCountAsync(workspaceId, token));
        }, cancellationToken);
}
