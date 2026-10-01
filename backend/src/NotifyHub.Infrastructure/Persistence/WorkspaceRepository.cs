using Microsoft.EntityFrameworkCore;
using NotifyHub.Application.Workspaces;
using NotifyHub.Domain.Workspaces;
namespace NotifyHub.Infrastructure.Persistence;
public sealed class WorkspaceRepository(NotifyHubDbContext db) : IWorkspaceRepository
{
    public Task<Workspace?> FindForUpdateAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("A transaction is required for membership mutation.");
        return db.Workspaces.FromSqlInterpolated($"SELECT *, xmin FROM workspaces WHERE id = {workspaceId} FOR UPDATE").SingleOrDefaultAsync(cancellationToken);
    }
    public void Add(Workspace workspace) => db.Workspaces.Add(workspace);
    public Task<Workspace?> FindAsync(Guid workspaceId, CancellationToken cancellationToken) => db.Workspaces.SingleOrDefaultAsync(x => x.Id == workspaceId, cancellationToken);
}
public sealed class UnitOfWork(NotifyHubDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
    public async Task ExecuteAsync(Func<CancellationToken, Task> command, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await command(cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();
            throw;
        }
    }
}



