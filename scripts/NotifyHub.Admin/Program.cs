using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotifyHub.Application.Identity;
using NotifyHub.Infrastructure;

try
{
    if (args.Length == 1 && args[0] == "init-test-tls")
    {
        TestTlsSetup.Run();
        return;
    }
    if (args.Length == 1 && args[0] == "init-signing-key")
    {
        SigningKeySetup.Run();
        return;
    }
    if (args.Length != 1 || args[0] != "bootstrap")
    {
        Console.WriteLine("Run from the NotifyHub root: dotnet run --project scripts/NotifyHub.Admin -- bootstrap");
        Console.WriteLine("Create a local signing key: dotnet run --project scripts/NotifyHub.Admin -- init-signing-key");
        return;
    }
    if (Console.IsInputRedirected)
        throw new InvalidOperationException("Bootstrap requires an interactive terminal; passwords are never accepted as command arguments.");
    Console.Write("Administrator email: ");
    var email = Console.ReadLine() ?? "";
    var password = ReadPassword("Password (12–256 characters): ");
    var confirmation = ReadPassword("Confirm password: ");
    if (password != confirmation) throw new ArgumentException("Passwords do not match.");
    var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("backend/src/NotifyHub.Api/appsettings.json", optional: false)
        .AddJsonFile("backend/src/NotifyHub.Api/appsettings.Local.json", optional: true)
        .AddEnvironmentVariables().Build();
    var services = new ServiceCollection();
    // CLI does not emit provider/EF exception details or credential-bearing SQL logs.
    services.AddLogging(options => options.ClearProviders());
    services.AddInfrastructure(configuration);
    await using var provider = services.BuildServiceProvider();
    await using var scope = provider.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<IAccountBootstrap>()
        .CreateAdministratorAsync(email, password, CancellationToken.None);
    Console.WriteLine("Initial administrator created. MFA enrollment is required before privileged access.");
}
catch (Exception exception)
{
    Console.Error.WriteLine("Identity setup failed (" + exception.GetType().Name +
        "). Details suppressed; verify the command, local settings, permissions and whether setup was already completed.");
    Environment.ExitCode = 1;
}

static string ReadPassword(string prompt)
{
    Console.Write(prompt);
    var value = new StringBuilder();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); return value.ToString(); }
        if (key.Key == ConsoleKey.Backspace) { if (value.Length > 0) value.Length--; continue; }
        if (!char.IsControl(key.KeyChar))
        {
            if (value.Length >= 256) throw new ArgumentException("Password exceeds the maximum length.");
            value.Append(key.KeyChar);
        }
    }
}