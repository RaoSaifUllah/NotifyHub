namespace NotifyHub.Application.Identity;

public interface IAccountBootstrap
{
    Task<Guid> CreateAdministratorAsync(string email, string password, CancellationToken cancellationToken);
}