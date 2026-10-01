namespace NotifyHub.Domain.Workspaces;
public enum WorkspaceRole { ReadOnly, Developer, Owner }
public enum WorkspacePermission { Read, ManageResources, ManageMembers }
public static class WorkspacePermissions
{
    public static bool Allows(WorkspaceRole role, WorkspacePermission permission) => permission switch
    {
        WorkspacePermission.Read => Enum.IsDefined(role),
        WorkspacePermission.ManageResources => role is WorkspaceRole.Developer or WorkspaceRole.Owner,
        WorkspacePermission.ManageMembers => role == WorkspaceRole.Owner,
        _ => false
    };
}
public sealed class WorkspaceMember
{
    private WorkspaceMember() { }
    public Guid WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }
    public WorkspaceRole Role { get; private set; }
    public uint Version { get; private set; }
    public static WorkspaceMember Create(Guid workspaceId, Guid userId, WorkspaceRole role)
    {
        if (workspaceId == Guid.Empty || userId == Guid.Empty || !Enum.IsDefined(role))
            throw new ArgumentException("Valid workspace, user and role are required.");
        return new WorkspaceMember { WorkspaceId = workspaceId, UserId = userId, Role = role };
    }
    public void ChangeRole(WorkspaceRole nextRole, int currentOwnerCount)
    {
        if (!Enum.IsDefined(nextRole)) throw new ArgumentException("Invalid role.");
        if (Role == WorkspaceRole.Owner && nextRole != WorkspaceRole.Owner && currentOwnerCount <= 1)
            throw new InvalidOperationException("The last owner cannot be removed.");
        Role = nextRole;
    }
}
