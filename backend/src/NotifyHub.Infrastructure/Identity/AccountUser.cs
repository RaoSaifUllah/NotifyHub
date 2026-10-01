using Microsoft.AspNetCore.Identity;
namespace NotifyHub.Infrastructure.Identity;
public sealed class AccountUser : IdentityUser<Guid>
{
    public bool IsSystemAdministrator { get; set; }
    public bool Disabled { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
