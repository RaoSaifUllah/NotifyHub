namespace NotifyHub.Application.Identity;
public sealed record AccountCreation(Guid UserId, bool Succeeded, IReadOnlyList<string> ErrorCodes);
public interface IAccountRepository
{
    Task<AccountCreation> CreateAsync(string email, string password, DateTimeOffset now, CancellationToken cancellationToken);
}
