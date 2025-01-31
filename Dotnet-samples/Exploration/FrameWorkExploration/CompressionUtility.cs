using System;
using System.IO;
using System.IO.Compression;

public class CompressionUtility {

    public void Run() {
        try {
            // Zip a single file
            CreateZip(
                @"C:\Workspace\Logs\logs\testfile.csv",
                @"C:\Workspace\Logs\logs\testfile.zip");

            // Extract zip
            ExtractZip(
                @"C:\Workspace\Logs\logs\testfile.zip",
                @"C:\Workspace\Logs\logs\"
            );

            Console.WriteLine("All compression operations completed successfully.");
        } catch (Exception ex) {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    public static void CreateZip(string sourceFile, string destinationZipFile) {
        try {
            using (var zip = ZipFile.Open(destinationZipFile, ZipArchiveMode.Create)) {
                zip.CreateEntryFromFile(sourceFile, Path.GetFileName(sourceFile), CompressionLevel.Fastest);
            }
        } catch (Exception ex) {
            throw new Exception($"Error creating zip file: {ex.Message}", ex);
        }
    }

    public static void ExtractZip(string zipFile, string destinationDirectory) {
        try {
            var filePath = Path.GetDirectoryName(zipFile);
            var file = Path.GetFileName(zipFile).Replace(".zip", ".csv");
            var fileName = Path.Combine(filePath, file);

            ZipFile.ExtractToDirectory(zipFile, destinationDirectory);
        } catch (Exception ex) {
            throw new Exception($"Error extracting zip: {ex.Message}", ex);
        }
    }
}