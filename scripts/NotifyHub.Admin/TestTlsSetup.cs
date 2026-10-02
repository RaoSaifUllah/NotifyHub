using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

internal static class TestTlsSetup
{
    public static void Run()
    {
        var root = Directory.GetCurrentDirectory();
        if (!File.Exists(Path.Combine(root, "NotifyHub.slnx"))) root = Path.GetFullPath(Path.Combine(root, ".."));
        if (!File.Exists(Path.Combine(root, "NotifyHub.slnx")))
            throw new InvalidOperationException("Run from NotifyHub or its Frontend directory.");
        var directory = Path.Combine(root, "TestResults", "tls");
        Directory.CreateDirectory(directory);
        using var rsa = RSA.Create(3072);
        var request = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var names = new SubjectAlternativeNameBuilder();
        names.AddDnsName("localhost");
        names.AddIpAddress(IPAddress.Loopback);
        request.CertificateExtensions.Add(names.Build());
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, true));
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(1));
        File.WriteAllText(Path.Combine(directory, "server-cert.pem"), certificate.ExportCertificatePem());
        File.WriteAllText(Path.Combine(directory, "server-key.pem"), rsa.ExportPkcs8PrivateKeyPem());
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            File.SetUnixFileMode(Path.Combine(directory, "server-key.pem"), UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        Console.WriteLine("Disposable HTTPS browser-test certificate created in ignored TestResults; no trust store changed.");
    }
}