using Dapper;
using Npgsql;
using NotifyHub.Application.Workspaces;
namespace NotifyHub.Infrastructure.Persistence;
public sealed class WorkspaceQueries(string readConnectionString, string schema = "public") : IWorkspaceQueries
{
    public async Task<WorkspaceSummary?> FindAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        var quotedSchema = new NpgsqlCommandBuilder().QuoteIdentifier(schema);
        await using var connection = new NpgsqlConnection(readConnectionString);
        var row = await connection.QuerySingleOrDefaultAsync<WorkspaceRow>(new CommandDefinition(
            $"SELECT id AS Id, name AS Name, time_zone AS TimeZone, created_at AS CreatedAt FROM {quotedSchema}.workspace_summaries WHERE id = @WorkspaceId",
            new { WorkspaceId = workspaceId }, cancellationToken: cancellationToken));
        return row is null ? null : new WorkspaceSummary(row.Id, row.Name, row.TimeZone,
            new DateTimeOffset(DateTime.SpecifyKind(row.CreatedAt, DateTimeKind.Utc)));
    }
    private sealed class WorkspaceRow
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string TimeZone { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}

