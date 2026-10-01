using NotifyHub.Domain.Workspaces;
namespace NotifyHub.UnitTests;
public class WorkspaceTests
{
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void RejectsEmptyNames(string name) => Assert.Throws<ArgumentException>(() => Workspace.Create(name, "UTC", DateTimeOffset.UtcNow));
    [Fact]
    public void RejectsOversizedName() => Assert.Throws<ArgumentException>(() => Workspace.Create(new string('a', 101), "UTC", DateTimeOffset.UtcNow));
    [Fact]
    public void RejectsInvalidTimezone() => Assert.Throws<ArgumentException>(() => Workspace.Create("Work", "invalid", DateTimeOffset.UtcNow));
    [Fact]
    public void NormalizesNameAndTimestamp()
    {
        var workspace = Workspace.Create(" Work ", "Asia/Karachi", new DateTimeOffset(2026, 10, 1, 21, 0, 0, TimeSpan.FromHours(5)));
        Assert.Equal("Work", workspace.Name);
        Assert.Equal(TimeSpan.Zero, workspace.CreatedAt.Offset);
        Assert.NotEqual(Guid.Empty, workspace.Id);
    }
    [Fact]
    public void DependencyBoundaries()
    {
        foreach (var assembly in new[] { typeof(Workspace).Assembly, typeof(NotifyHub.Application.Workspaces.IUnitOfWork).Assembly })
            Assert.DoesNotContain(assembly.GetReferencedAssemblies(), x => x.Name!.Contains("EntityFramework") || x.Name.Contains("Infrastructure") || x.Name.Contains("AspNetCore"));
    }
}
