// See https://aka.ms/new-console-template for more information

using Exploration;

using System.Security.Cryptography;
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


//using (Aes aes = Aes.Create()) {
//    // Generate a new AES key and IV
//    aes.GenerateKey();
//    aes.GenerateIV();

//    // Get the key and IV
//    byte[] key = aes.Key;
//    byte[] iv = aes.IV;
//    // Convert the key and IV to Base64 strings
//    string base64Key = Convert.ToBase64String(key);
//    string base64IV = Convert.ToBase64String(iv);
//}