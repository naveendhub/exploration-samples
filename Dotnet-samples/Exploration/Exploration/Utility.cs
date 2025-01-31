using System.Security.Cryptography;

namespace Exploration {
    internal class Utility {
        public void Run()
        {
            using (Aes aes = Aes.Create()) {
                // Generate a new AES key and IV
                aes.GenerateKey();
                aes.GenerateIV();

                // Get the key and IV
                byte[] key = aes.Key;
                byte[] iv = aes.IV;
                // Convert the key and IV to Base64 strings
                string base64Key = Convert.ToBase64String(key);
                string base64IV = Convert.ToBase64String(iv);
            }

            var osVersion = Environment.OSVersion;
            Console.WriteLine($"OS Version: {osVersion}");
            if (osVersion.Platform == PlatformID.Win32NT) {
                Console.WriteLine("Running on Windows");
            } else {
                Console.WriteLine("Not running on Windows");
            }
        }
    }
}
