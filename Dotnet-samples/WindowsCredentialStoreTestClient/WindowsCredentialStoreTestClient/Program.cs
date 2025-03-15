using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsCredentialStoreTestClient {

    public class Program {
        static void Main(string[] args) {
            var vault = new WindowsPasswordVault();

            if (args.Length == 0 || args[0].ToLower() == "--help") {
                ShowHelp();
                return;
            }
            
            switch (args[0].ToLower()) {
                case "--store":
                    if (args.Length != 2 || !args[1].Contains(":") || !args[1].Contains("@")) {
                        Console.WriteLine("Invalid arguments for --store. Use format: username:password@targetName");
                    } else {
                        var parts = args[1].Split(new[] { ':', '@' }, 3);
                        var username = parts[0];
                        var password = parts[1];
                        var targetName = parts[2];
                        vault.StoreCredential(targetName, username, password);

                        Console.WriteLine("Credential stored successfully.");
                    }
                    break;

                case "--delete":
                    if (args.Length != 2) {
                        Console.WriteLine("Invalid arguments for --delete. Use format: --delete targetName");
                    } else {
                        var targetName = args[1];

                        vault.DeleteCredential(targetName);
                        Console.WriteLine("Credential deleted successfully.");
                    }
                    break;

                case "--check":
                    if (args.Length != 2) {
                        Console.WriteLine("Invalid arguments for --check. Use format: --check targetName");
                    } else {
                        var targetName = args[1];
                        bool exists = vault.CredentialExists(targetName);
                        Console.WriteLine(exists ? "Credential found." : "Credential not found.");
                    }
                    break;

                case "--read":
                    if (args.Length != 2) {
                        Console.WriteLine("Invalid arguments for --read. Use format: --read targetName");
                    } else {
                        var targetName = args[1];

                        (string username, string password) = vault.RetrieveCredential(targetName);
                        Console.WriteLine($"Username: {username}");
                        Console.WriteLine($"Password: {password}");

                    }
                    break;

                default:
                    Console.WriteLine("Invalid command.");
                    break;
            }

            Console.ReadLine();
        }
        private static void ShowHelp() {
            Console.WriteLine("Usage:");
            Console.WriteLine("  --store username:password@targetName  Store a new credential");
            Console.WriteLine("  --delete targetName                   Delete a credential");
            Console.WriteLine("  --check targetName                    Check if a credential exists");
            Console.WriteLine("  --read targetName                     Read a credential");
            Console.WriteLine("  --help                                Show this help message");
        }
    }
}