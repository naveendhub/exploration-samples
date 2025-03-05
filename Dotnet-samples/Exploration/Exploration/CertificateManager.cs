using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Exploration {
    internal static class CertificateManager {

        internal static void Run() {
            // Open the local machine's certificate store
            const string message = "Naveen-plaintext";
            using (X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine)) {
                store.Open(OpenFlags.ReadOnly);

                // Find the certificate by its subject name
                string certificateName = "IOCC-1-Issue-1";
                X509Certificate2Collection certificates = store.Certificates.Find(X509FindType.FindBySubjectName, certificateName, false);

                if (certificates.Count > 0) {
                    // Certificate found
                    X509Certificate2 certificate = certificates[0];
                    Console.WriteLine("Certificate found: " + certificate.Subject);
                    if (certificate.HasPrivateKey) {
                        Console.WriteLine("Certificate has private key.");
                    } else {
                        Console.WriteLine("Certificate does not have private key.");
                    }
                    string encryptedMessage = string.Empty;
                    using (var key = certificate.GetRSAPublicKey()) {
                        var encryptedBytes = key.Encrypt(System.Text.Encoding.UTF8.GetBytes(message), RSAEncryptionPadding.OaepSHA256);
                        encryptedMessage = Convert.ToBase64String(encryptedBytes);
                        Console.WriteLine("Encrypted message: " + encryptedMessage);
                    }

                    var passwordVault = new PasswordVault();
                    passwordVault.StoreCredential("exploration", "certificate", encryptedMessage);

                    (string iv, string aesKey) = passwordVault.RetrieveCredential("exploration");

                    Console.WriteLine($"Iv {iv} aes {aesKey}");

                    using (var key = certificate.GetRSAPrivateKey()) {
                        var decryptedBytes = key.Decrypt(Convert.FromBase64String(aesKey), RSAEncryptionPadding.OaepSHA256);
                        var decryptedMessage = System.Text.Encoding.UTF8.GetString(decryptedBytes);
                        Console.WriteLine("Decrypted message: " + decryptedMessage);
                    }


                } else {
                    // Certificate not found
                    Console.WriteLine("Certificate not found.");
                }

                store.Close();
            }
        }

    }
}
