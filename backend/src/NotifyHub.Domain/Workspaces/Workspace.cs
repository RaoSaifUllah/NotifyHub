namespace NotifyHub.Domain.Workspaces;
public sealed class Workspace
{
    private Workspace() { }
    public Guid Id { get; private set; }
    public string Name { get; private set; } = "";
    public string TimeZone { get; private set; } = "UTC";
    public DateTimeOffset CreatedAt { get; private set; }
    public uint Version { get; private set; }
    public static Workspace Create(string name, string timeZone, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100)
            throw new ArgumentException("Workspace name must contain 1–100 characters.", nameof(name));
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out _) || (timeZone != "UTC" && !TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZone, out _)))
            throw new ArgumentException("An IANA timezone is required.", nameof(timeZone));
        return new Workspace { Id = Guid.NewGuid(), Name = name.Trim(), TimeZone = timeZone, CreatedAt = now.ToUniversalTime() };
    }
}
