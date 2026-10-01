using NotifyHub.Domain.Workspaces;
namespace NotifyHub.Application.Workspaces;
public interface IWorkspaceRepository
{
    void Add(Workspace workspace);
    Task<Workspace?> FindForUpdateAsync(Guid workspaceId, CancellationToken cancellationToken);
    Task<Workspace?> FindAsync(Guid workspaceId, CancellationToken cancellationToken);
}
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    Task ExecuteAsync(Func<CancellationToken, Task> command, CancellationToken cancellationToken);
}
public sealed record WorkspaceSummary(Guid Id, string Name, string TimeZone, DateTimeOffset CreatedAt);
// Callers must authorize workspaceId before invoking a projection.
public interface IWorkspaceQueries
{
    Task<WorkspaceSummary?> FindAsync(Guid workspaceId, CancellationToken cancellationToken);
}

