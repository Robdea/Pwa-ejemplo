using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

const string ip = "192.168.1.69";
const string pfxPass = "NotesDemo2026!";

// C:\cagada\NotesDemo\tools\CertGen -> subir 2 niveles -> C:\cagada\NotesDemo\certs
string outDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "certs"));
Directory.CreateDirectory(outDir);

using var rsa = RSA.Create(2048);
var req = new CertificateRequest($"CN={ip}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

var san = new SubjectAlternativeNameBuilder();
san.AddIpAddress(IPAddress.Parse(ip));
san.AddDnsName("localhost");
san.AddDnsName("127.0.0.1");
req.CertificateExtensions.Add(san.Build());

req.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
req.CertificateExtensions.Add(new X509KeyUsageExtension(
    X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
    new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));

using var cert = req.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddYears(3));

Console.WriteLine($"THUMBPRINT={cert.Thumbprint}");
Console.WriteLine($"SUBJECT={cert.Subject}");
Console.WriteLine($"NOTAFTER={cert.NotAfter:yyyy-MM-dd}");
foreach (var e in cert.Extensions)
{
    var sink = new AsnEncodedData(e.Oid, e.RawData);
    Console.WriteLine($"EXT {e.Oid.Value} => {sink.Format(false)}");
}

var pfx = Path.Combine(outDir, "notes-lan.pfx");
var cer = Path.Combine(outDir, "notes-lan.cer");
File.WriteAllBytes(pfx, cert.Export(X509ContentType.Pfx, pfxPass));
File.WriteAllBytes(cer, cert.Export(X509ContentType.Cert));
Console.WriteLine($"PFX={pfx}");
Console.WriteLine($"CER={cer}");
