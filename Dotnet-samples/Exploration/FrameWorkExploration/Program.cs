using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Exploration;

namespace FrameWorkExploration {
    internal class Program {
        static void Main(string[] args) {
            //var credentialManager = new WindowsCredentialsStoreManager();
            ////credentialManager.StoreCredentials();
            ////Console.WriteLine("Credentials stored successfully");

            //credentialManager.ReadCredential("MyApp");
            //credentialManager.DeleteCredential("MyApp");

            //var credManager = new PasswordVault();

            //try {
            //    // Store credentials
            //    credManager.StoreCredential(
            //        "MyApplication",
            //        "user@example.com",
            //        "SecurePassword123"
            //    );
            //    Console.WriteLine("Credentials stored successfully");

            //    // Check if credentials exist
            //    bool exists = credManager.CredentialExists("MyApplication");
            //    Console.WriteLine($"Credentials exist: {exists}");

            //    // Retrieve credentials
            //    var (username, password) = credManager.RetrieveCredential("MyApplication");
            //    Console.WriteLine($"Retrieved Username: {username}");
            //    Console.WriteLine($"Retrieved Password: {password}");

            //    // Delete credentials
            //    credManager.DeleteCredential("MyApplication");
            //    Console.WriteLine("Credentials deleted successfully");
            //} catch (Exception ex) {
            //    Console.WriteLine($"Error: {ex.Message}");
            //}

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

            try {
                // Zip a single file
                CompressionUtility.CreateZip(
                    @"C:\Workspace\Logs\logs\testfile.csv",
                    @"C:\Workspace\Logs\logs\testfile.zip");

               
                //// Zip multiple files
                //string[] files = new[]
                //{
                //    @"C:\Files\file1.txt",
                //    @"C:\Files\file2.txt",
                //    @"C:\Files\file3.txt"
                //};
                //CompressionUtility.CreateZipWithMultipleFiles(
                //    files,
                //    @"C:\Files\multiple.zip"
                //);

                //// Zip a directory
                //CompressionUtility.CreateZipFromDirectory(
                //    @"C:\Files\MyFolder",
                //    @"C:\Files\folder.zip"
                //);

                //// Add to existing zip
                //CompressionUtility.AddToExistingZip(
                //    @"C:\Files\existing.zip",
                //    @"C:\Files\newfile.txt"
                //);

                // Extract zip
                CompressionUtility.ExtractZip(
                    @"C:\Workspace\Logs\logs\testfile.zip",
                    @"C:\Workspace\Logs\logs\"
                );

                // Create zip with custom content
                //CompressionUtility.CreateZipWithCustomContent(
                //    @"C:\Files\custom.zip",
                //    "readme.txt",
                //    "This is a custom text file inside the zip."
                //);

                Console.WriteLine("All compression operations completed successfully.");
            } catch (Exception ex) {
                Console.WriteLine($"Error: {ex.Message}");
            }

            Console.ReadLine();
        }
    }
}
