using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using NotifyHub.Domain.Workspaces;
using NotifyHub.Infrastructure.Identity;
using Xunit.Abstractions;
namespace NotifyHub.UnitTests;
public class IdentityFoundationTests(ITestOutputHelper output)
{
    [Fact]
    public void PasswordHashesAreSaltedAndRejectWrongPasswords()
    {
        var hasher = new Argon2PasswordHasher();
        var user = new AccountUser();
        var clock = Stopwatch.StartNew();
        var first = hasher.HashPassword(user, "local-test-password-only");
        clock.Stop();
        output.WriteLine($"Argon2id 64MiB t=3 p=1 single hash: {clock.ElapsedMilliseconds}ms; logical processors: {Environment.ProcessorCount}");
        var second = hasher.HashPassword(user, "local-test-password-only");
        Assert.NotEqual(first, second);
        Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(user, first, "local-test-password-only"));
        Assert.Equal(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword(user, first, "incorrect"));
    }
    [Theory]
    [InlineData("malformed")]
    [InlineData("$argon2id$v=19$m=999999999,t=3,p=1$AAAA$AAAA")]
    [InlineData("$argon2id$v=19$m=65536,t=999999999,p=1$AAAA$AAAA")]
    public void CorruptOrExcessiveHashCostFailsBeforeAllocation(string hash) =>
        Assert.Equal(PasswordVerificationResult.Failed, new Argon2PasswordHasher().VerifyHashedPassword(new AccountUser(), hash, "test"));
    [Fact]
    public void ReadOnlyCannotMutateAndDeveloperCannotManageMembership()
    {
        Assert.True(WorkspacePermissions.Allows(WorkspaceRole.ReadOnly, WorkspacePermission.Read));
        Assert.False(WorkspacePermissions.Allows(WorkspaceRole.ReadOnly, WorkspacePermission.ManageResources));
        Assert.True(WorkspacePermissions.Allows(WorkspaceRole.Developer, WorkspacePermission.ManageResources));
        Assert.False(WorkspacePermissions.Allows(WorkspaceRole.Developer, WorkspacePermission.ManageMembers));
        Assert.False(WorkspacePermissions.Allows((WorkspaceRole)999, WorkspacePermission.Read));
    }
    [Fact]
    public void LastOwnerCannotBeDemoted()
    {
        var member = WorkspaceMember.Create(Guid.NewGuid(), Guid.NewGuid(), WorkspaceRole.Owner);
        Assert.Throws<InvalidOperationException>(() => member.ChangeRole(WorkspaceRole.Developer, 1));
        member.ChangeRole(WorkspaceRole.Developer, 2);
        Assert.Equal(WorkspaceRole.Developer, member.Role);
    }
}
