using Microsoft.AspNetCore.Identity;
using NotifyHub.Application.Identity;
namespace NotifyHub.Infrastructure.Identity;
public sealed class AccountRepository(UserManager<AccountUser> users) : IAccountRepository
{
    public async Task<AccountCreation> CreateAsync(string email, string password, DateTimeOffset now, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (email.Length > 254 || password.Length is < 12 or > 256)
            return new(Guid.Empty, false, ["INVALID_ACCOUNT_INPUT"]);
        var user = new AccountUser { Id = Guid.NewGuid(), Email = email.Trim(), UserName = email.Trim(), CreatedAt = now.ToUniversalTime() };
        var result = await users.CreateAsync(user, password);
        // Store autosave is disabled; caller must commit through IUnitOfWork.
        return new(user.Id, result.Succeeded, result.Errors.Select(x => x.Code).ToArray());
    }
}
