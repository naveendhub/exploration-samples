using System;

using Windows.Security.Credentials;

namespace Exploration {
    internal class PasswordManager {

        public void Run() {
            //var credentialManager = new WindowsCredentialsStoreManager();
            ////credentialManager.StoreCredentials();
            ////Console.WriteLine("Credentials stored successfully");

            //credentialManager.ReadCredential("MyApp");
            //credentialManager.DeleteCredential("MyApp");
            //Console.ReadLine();


            var credManager = new PasswordVault();

            try {
                // Store credentials
                credManager.StoreCredential(
                    "MyApplication",
                    "user@example.com",
                    "SecurePassword123"
                );
                credManager.StoreCredential(
                    "MyApplication",
                    "user1@example.com",
                    "SecurePassword123"
                );

                Console.WriteLine("Credentials stored successfully");

                // Check if credentials exist
                bool exists = credManager.CredentialExists("MyApplication");
                Console.WriteLine($"Credentials exist: {exists}");

                // Retrieve credentials
                var (username, password) = credManager.RetrieveCredential("MyApplication");
                Console.WriteLine($"Retrieved Username: {username}");
                Console.WriteLine($"Retrieved Password: {password}");

                // Delete credentials
                credManager.DeleteCredential("MyApplication");
                Console.WriteLine("Credentials deleted successfully");
            } catch (Exception ex) {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        public void ExplorePasswordVault() {
            AddCredential("MyApp", "Naveen", "P167");
            AddCredential("MyApp", "Naveen2", "P1672");
            (string userName, string pwd) = RetrieveCredentials("MyApp");
            Console.WriteLine($"Username: {userName}, Password: {pwd}");

            DeleteCredentials("MyApp", "Naveen");

        }
        public void AddCredential(string resource, string username, string password) {
            Windows.Security.Credentials.PasswordVault vault =
                new Windows.Security.Credentials.PasswordVault();
            // Add the new credential
            var credential = new PasswordCredential(
                    resource,
                    username,
                    password
                );

            vault.Add(credential);
        }

        public (string username, string password) RetrieveCredentials(string resource) {
            Windows.Security.Credentials.PasswordVault vault =
                new Windows.Security.Credentials.PasswordVault();
            try {
                // Get all credentials for this resource
                var credentials = vault.FindAllByResource(resource);

                if (credentials.Count == 0) {
                    throw new Exception($"No credentials found for resource: {resource}");
                }

                // Get the first credential (you might want to handle multiple credentials differently)
                var credential = credentials[0];

                // Need to retrieve password explicitly
                credential.RetrievePassword();

                return (credential.UserName, credential.Password);
            } catch (Exception ex) {
                throw new Exception($"Error retrieving credentials: {ex.Message}");
            }
        }
        public void DeleteCredentials(string appName, string userName) {
            Windows.Security.Credentials.PasswordVault vault =
                new Windows.Security.Credentials.PasswordVault();
            try {
                var credential = vault.Retrieve(appName, userName);
                if (credential != null) {
                    vault.Remove(credential);
                }
            } catch (Exception ex) {
                throw new Exception($"Error deleting credentials: {ex.Message}");
            }
        }

    }
}
