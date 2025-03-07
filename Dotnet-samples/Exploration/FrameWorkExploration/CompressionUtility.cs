using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using FrameWorkExploration;

public class CompressionUtility {

    public void Run() {
        try {
            // Zip a single file
            CreateZip(
                @"C:\Data\Logs\System\System_20250307113846090.csv",
                @"C:\Data\Logs\System\System_20250307113846090.zip");

            // Extract zip
            //ExtractZip(
            //    @"C:\Workspace\Logs\logs\testfile.zip",
            //    @"C:\Workspace\Logs\logs\"
            //);

            var fileName = Path.GetFileNameWithoutExtension(@"C:\Data\Logs\System\System_20250307113846090.zip") + ".csv";

            using (var stream = GetFileStreamFromZip(@"C:\Data\Logs\System\System_20250307113846090.zip", fileName))
            using (var reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    // Process one line at a time
                    Console.WriteLine(line);
                }
            }

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


    public static Stream GetFileStreamFromZip(string zipFilePath, string entryName)
    {
        try
        {
            // Create a ZipArchive that stays open
            var archive = ZipFile.OpenRead(zipFilePath);

            // Find the specific entry
            var entry = archive.Entries.FirstOrDefault(e => e.Name == entryName);

            if (entry == null)
            {
                archive.Dispose();
                throw new FileNotFoundException($"File '{entryName}' not found in the zip archive.");
            }

            // Open a stream to the entry
            return new DisposableStreamWrapper(entry.Open(), archive);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error reading zip file: {ex.Message}", ex);
        }
    }

}