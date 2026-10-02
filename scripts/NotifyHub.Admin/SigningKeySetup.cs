using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

internal static class SigningKeySetup
{
    public static void Run()
    {
        var root = Directory.GetCurrentDirectory();
        if (!File.Exists(Path.Combine(root, "NotifyHub.slnx")))
            throw new InvalidOperationException("Run from the NotifyHub root.");
        var configurationPath = Path.Combine(root, "backend/src/NotifyHub.Api/appsettings.Local.json");
        var document = File.Exists(configurationPath) ? JsonNode.Parse(File.ReadAllText(configurationPath)) as JsonObject : new JsonObject();
        if (document is null) throw new InvalidOperationException("Local configuration is invalid.");
        if (document["Security"]?["Jwt"]?["PrivateKeyPath"] is JsonValue configured &&
            !string.IsNullOrWhiteSpace(configured.GetValue<string>()))
            throw new InvalidOperationException("A signing key is already configured; use a reviewed rotation procedure.");
        var directory = new DirectoryInfo(Path.Combine(root, "secrets", "jwt"));
        directory.Create();
        if (OperatingSystem.IsWindows())
        {
            using var identity = WindowsIdentity.GetCurrent();
            var owner = identity.User ?? throw new InvalidOperationException("Cannot determine key owner.");
            var acl = new DirectorySecurity();
            acl.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
            acl.AddAccessRule(new FileSystemAccessRule(owner, FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None, AccessControlType.Allow));
            directory.SetAccessControl(acl);
        }
        else
        {
            File.SetUnixFileMode(directory.FullName, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
        var keyPath = Path.Combine(directory.FullName, "private.pem");
        using var rsa = RSA.Create(3072);
        var bytes = Encoding.UTF8.GetBytes(rsa.ExportPkcs8PrivateKeyPem());
        try
        {
            var fileOptions = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None };
            if (!OperatingSystem.IsWindows()) fileOptions.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            using (var stream = new FileStream(keyPath, fileOptions))
            stream.Write(bytes);
            var security = document["Security"] as JsonObject;
            if (security is null) { security = new JsonObject(); document["Security"] = security; }
            var jwt = security["Jwt"] as JsonObject;
            if (jwt is null) { jwt = new JsonObject(); security["Jwt"] = jwt; }
            jwt["PrivateKeyPath"] = keyPath;
            jwt["Issuer"] ??= "NotifyHub";
            jwt["Audience"] ??= "NotifyHub.Web";
            var temporaryPath = Path.Combine(directory.FullName, "local-settings-" + Guid.NewGuid().ToString("N") + ".tmp");
            File.WriteAllText(temporaryPath, document.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(temporaryPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            File.Move(temporaryPath, configurationPath, overwrite: true);
        }
        finally { CryptographicOperations.ZeroMemory(bytes); }
        Console.WriteLine("Local signing key created with owner-only directory access and configured in ignored local settings.");
        Console.WriteLine("Configure HTTPS and Security:BrowserOrigins separately. Production rotation/restore is still required.");
    }
}