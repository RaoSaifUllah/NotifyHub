using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NotifyHub.Application.Identity;
using NotifyHub.Application.Workspaces;
using NotifyHub.Infrastructure.Persistence;

namespace NotifyHub.Infrastructure.Identity;

public sealed class AccountBootstrap(
    NotifyHubDbContext db, UserManager<AccountUser> users, IUnitOfWork unit, TimeProvider clock) : IAccountBootstrap
{
    public async Task<Guid> CreateAdministratorAsync(string email, string password, CancellationToken cancellationToken)
    {
        email = email.Trim();
        if (email.Length is < 3 or > 254 || !MailAddress.TryCreate(email, out var address) ||
            address.Address != email || password.Length is < 12 or > 256)
            throw new ArgumentException("Provide a valid email and a password of 12–256 characters.");
        Guid id = Guid.Empty;
        await unit.ExecuteAsync(async token =>
        {
            // Serializes bootstrap across CLI processes. Never reset or replace an existing account.
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(734267812905)", token);
            if (await db.Users.AnyAsync(token))
                throw new InvalidOperationException("Bootstrap is closed because an account already exists.");
            var account = new AccountUser
            {
                Id = Guid.NewGuid(), Email = email, UserName = email,
                EmailConfirmed = true, IsSystemAdministrator = true,
                LockoutEnabled = true, CreatedAt = clock.GetUtcNow()
            };
            var result = await users.CreateAsync(account, password);
            if (!result.Succeeded)
                throw new ArgumentException("Administrator credentials do not meet account policy.");
            id = account.Id;
        }, cancellationToken);
        return id;
    }
}