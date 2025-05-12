// Copyright Koninklijke Philips N.V. 2025

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace FrameWorkExploration {

    /// <summary>
    /// Represents a utility for decompressing files.
    /// </summary>
    //@AdapterType: Service - Utility for decompressing files.
    internal static class DecompressionUtility {
        /// <summary>
        /// Gets the stream and zip archive for the specified file path.
        /// </summary>
        /// <param name="filePath">The path of the file.</param>
        /// <returns>A tuple containing the stream and temp filename.</returns>
        /// <remarks>
        /// Caller needs to dispose stream and delete temp file
        /// </remarks>
        internal static (FileStream stream, string tempFilePath) GetStream(string filePath) {
            if (Path.GetExtension(filePath).Equals(".zip", StringComparison.OrdinalIgnoreCase)) {
                string entryName = Path.GetFileNameWithoutExtension(filePath) + ".csv";
                return GetFileStreamFromZip(filePath, entryName);
            }
            return (new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), string.Empty);
        }

        private static (FileStream stream, string tempFilePath) GetFileStreamFromZip(
            string zipFilePath, string entryName
        ) {
            string tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try {
                using (var archive = ZipFile.OpenRead(zipFilePath)) {
                    var entry = archive.Entries.FirstOrDefault(e => e.Name == entryName);
                    if (entry == null) {
                        throw new FileNotFoundException($"File '{entryName}' not found in the zip archive.");
                    }

                    // Extract to temp file
                    entry.ExtractToFile(tempFilePath, true);
                }

                // Create a FileStream that will be disposed by the caller
                var fileStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                // Create a wrapper that will delete the temp file when disposed
                return (fileStream, tempFilePath);

            } catch (Exception ex) {
                // Clean up on error
                DeleteFile(tempFilePath);
               throw new IOException("Error extracting file from zip archive.", ex);
            }
        }

        internal static void DeleteFile(string path) {
            try {
                if (File.Exists(path)) {
                    File.Delete(path);
                }
#pragma warning disable PFB4327
            } catch (Exception ex) {
            }
#pragma warning restore PFB4327
        }
    }
}
