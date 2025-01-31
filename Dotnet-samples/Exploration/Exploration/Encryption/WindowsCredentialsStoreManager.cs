using Meziantou.Framework.Win32;

namespace Exploration {
    internal class WindowsCredentialsStoreManager {

        public void StoreCredentials()
        {
            CredentialManager.WriteCredential(
                "MyApp", "naveend-test",
                "Test@123", CredentialPersistence.LocalMachine, CredentialType.Generic
                );

        }

        public Credential ReadCredential(string applicationName) {
            var credential = CredentialManager.ReadCredential(applicationName);
            if (credential == null) {
                Console.WriteLine("No credential found.");
                return null;
            }

            Console.WriteLine($"UserName: {credential.UserName}");
            Console.WriteLine($"Secret: {credential.Password}");
            Console.WriteLine($"Comment: {credential.Comment}");

            return credential;
        }

        public void DeleteCredential(string applicationName) {
            try {
                CredentialManager.DeleteCredential(applicationName);
                Console.WriteLine("Credential deleted successfully.");
            } catch (Exception ex) {
                Console.WriteLine($"Error deleting credential: {ex.Message}");
            }
        }

    }
}
