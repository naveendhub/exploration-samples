using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace StowClient
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            
            if (args.Length < 2 || string.IsNullOrWhiteSpace(args[0]) || string.IsNullOrWhiteSpace(args[1]))
            {
                Console.WriteLine("Please provide the input properly Usage: stowClient.exe <stowUrl> <ImageFolderPath>");
                return;
            }
            var stowUrl = args[0];
            var sourcePath = args[1];
            var stowClient = new StowClient();
            var stopWatch = Stopwatch.StartNew();
            var result = await stowClient.StoreDicomInDirectory(sourcePath, stowUrl, string.Empty);

            stopWatch.Stop();

            Console.WriteLine($"Images are stored with status is {result} and time taken {stopWatch.ElapsedMilliseconds} ms");
        }
    }
}