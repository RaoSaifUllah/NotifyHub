namespace NotifyHub.Application.Identity;

public sealed record LoginStart(string Status, string? ChallengeToken, string? CsrfToken,
    string? AuthenticatorKey, SessionCredentials? Credentials);
public sealed record LoginCompletion(SessionCredentials Credentials, string[] RecoveryCodes);
public interface ICredentialLogin
{
    Task<LoginStart?> BeginAsync(string email, string password, CancellationToken cancellationToken);
    Task<LoginCompletion?> CompleteMfaAsync(string challengeToken, string csrfToken, string code,
        bool recovery, CancellationToken cancellationToken);
}