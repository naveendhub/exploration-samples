using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LogRolloverCompressionService {
    public class LogRolloverCompressor {
        private readonly string _logDirectory;
        private readonly string _logFilePattern;
        private readonly int _pollingIntervalMs;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly HashSet<string> _knownLogFiles;
        private readonly List<string> _logFilesByRecentness;
        private readonly object _lockObject = new object();
        private bool _isRunning;
        private Task _pollingTask;
        private readonly ILogger _logger;

        public LogRolloverCompressor(
            string logDirectory,
            string logFilePattern = "*.log",
            int pollingIntervalMs = 5000, // 5 seconds default
            ILogger logger = null) {
            _logDirectory = logDirectory ?? throw new ArgumentNullException(nameof(logDirectory));
            _logFilePattern = logFilePattern;
            _pollingIntervalMs = pollingIntervalMs;
            _cancellationTokenSource = new CancellationTokenSource();
            _knownLogFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _logFilesByRecentness = new List<string>();
            _logger = logger ?? new ConsoleLogger();

            if (!Directory.Exists(_logDirectory)) {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        public void Start() {
            lock (_lockObject) {
                if (_isRunning) {
                    _logger.LogWarning("Log rollover compression service is already running.");
                    return;
                }

                _isRunning = true;

                // Initialize tracking for existing log files
                InitializeFileTracking();

                _pollingTask = Task.Run(() => PollForLogRolloversAsync(_cancellationTokenSource.Token));
                _logger.LogInformation($"Log rollover compression service started. Directory: {_logDirectory}, Pattern: {_logFilePattern}");
            }
        }

        private void InitializeFileTracking() {
            try {
                // Get all log files and sort them by last write time (newest first)
                var logFiles = Directory.GetFiles(_logDirectory, _logFilePattern)
                    .Select(f => new { Path = f, LastWrite = File.GetLastWriteTime(f) })
                    .OrderByDescending(f => f.LastWrite)
                    .Select(f => f.Path)
                    .ToList();

                // Add all files to our tracking collections
                foreach (string logFile in logFiles) {
                    _knownLogFiles.Add(logFile);
                    _logFilesByRecentness.Add(logFile);
                }

                _logger.LogInformation($"Initialized with {_knownLogFiles.Count} existing log files");
            } catch (Exception ex) {
                _logger.LogError($"Error initializing file tracking: {ex.Message}");
                // Initialize with empty collections to recover
                _knownLogFiles.Clear();
                _logFilesByRecentness.Clear();
            }
        }

        public async Task StopAsync() {
            lock (_lockObject) {
                if (!_isRunning) {
                    return;
                }

                _cancellationTokenSource.Cancel();
                _isRunning = false;
            }

            if (_pollingTask != null) {
                try {
                    await _pollingTask;
                } catch (OperationCanceledException) {
                    // Expected exception when cancellation is requested
                } catch (Exception ex) {
                    _logger.LogError($"Error stopping polling task: {ex.Message}");
                }
            }

            _logger.LogInformation("Log rollover compression service stopped.");
        }

        private async Task PollForLogRolloversAsync(CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {
                try {
                    // Get current log files in the directory
                    var currentLogFiles = Directory.GetFiles(_logDirectory, _logFilePattern);

                    // Detect new log files (not in our known files set)
                    var newLogFiles = new List<string>();
                    foreach (string logFile in currentLogFiles) {
                        if (!_knownLogFiles.Contains(logFile)) {
                            newLogFiles.Add(logFile);
                            _knownLogFiles.Add(logFile);
                        }
                    }

                    if (newLogFiles.Count > 0) {
                        _logger.LogInformation($"Detected {newLogFiles.Count} new log file(s) - possible rollover event");

                        // Sort new files by creation time (newest first)
                        newLogFiles = newLogFiles
                            .Select(f => new { Path = f, Creation = File.GetCreationTime(f) })
                            .OrderByDescending(f => f.Creation)
                            .Select(f => f.Path)
                            .ToList();

                        // Process each new file (possible rollover)
                        foreach (string newLogFile in newLogFiles) {
                            // Insert at the beginning of our recency list
                            _logFilesByRecentness.Insert(0, newLogFile);

                            // When a new log file appears, we want to compress all previous
                            // files except the most recent one (which might still be in use)
                            if (_logFilesByRecentness.Count >= 2) {
                                // The second file is the previous log file that should be compressed
                                string previousLogFile = _logFilesByRecentness[1];

                                if (File.Exists(previousLogFile)) {
                                    _logger.LogInformation($"Log rollover detected. New file: {newLogFile}. Compressing previous file: {previousLogFile}");
                                    await CompressLogFileAsync(previousLogFile);
                                }
                            }
                        }
                    }

                    // Clean up our tracking lists - remove entries for files that no longer exist
                    CleanupFileLists(currentLogFiles);
                } catch (Exception ex) {
                    _logger.LogError($"Error during log rollover detection: {ex.Message}");
                }

                // Wait for the next polling interval
                try {
                    await Task.Delay(_pollingIntervalMs, cancellationToken);
                } catch (OperationCanceledException) {
                    break;
                }
            }
        }

        private void CleanupFileLists(string[] currentLogFiles) {
            // Convert current files to a HashSet for faster lookups
            var currentFilesSet = new HashSet<string>(currentLogFiles, StringComparer.OrdinalIgnoreCase);

            // Remove tracking for files that no longer exist
            _knownLogFiles.RemoveWhere(file => !currentFilesSet.Contains(file));

            // Update recency list
            _logFilesByRecentness.RemoveAll(file => !currentFilesSet.Contains(file));
        }

        private async Task CompressLogFileAsync(string logFilePath) {
            try {
                string fileName = Path.GetFileNameWithoutExtension(logFilePath);
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string compressedFileName = $"{fileName}_{timestamp}.zip";
                string compressedFilePath = Path.Combine(_logDirectory, compressedFileName);

                bool compressionSuccessful = false;
                int retryCount = 0;
                const int maxRetries = 3;

                while (!compressionSuccessful && retryCount < maxRetries) {
                    try {
                        retryCount++;

                        // Ensure we have exclusive access to the file before compressing
                        using (FileStream originalFileStream = new FileStream(
                            logFilePath, FileMode.Open, FileAccess.Read, FileShare.None)) {
                            // Create a new zip archive
                            using (FileStream compressedFileStream = new FileStream(
                                compressedFilePath, FileMode.Create)) {
                                using (ZipArchive archive = new ZipArchive(compressedFileStream, ZipArchiveMode.Create)) {
                                    // Create a zip entry for the log file
                                    ZipArchiveEntry entry = archive.CreateEntry(Path.GetFileName(logFilePath));

                                    // Write the log file content to the zip entry
                                    using (Stream entryStream = entry.Open()) {
                                        await originalFileStream.CopyToAsync(entryStream);
                                    }
                                }
                            }
                        }
                        
                        // If we get here, compression was successful
                        compressionSuccessful = true;

                        // Delete the original log file after successful compression
                        File.Delete(logFilePath);
                        _logger.LogInformation($"Previous log file {logFilePath} compressed to {compressedFilePath} and original deleted");
                    } catch (IOException ex) {
                        if (retryCount < maxRetries) {
                            _logger.LogWarning($"Could not access file {logFilePath} for compression, retry {retryCount}/{maxRetries}: {ex.Message}");
                            await Task.Delay(1000); // Wait 1 second before retrying
                        } else {
                            _logger.LogError($"Failed to compress file {logFilePath} after {maxRetries} attempts: {ex.Message}");
                        }
                    }
                }
            } catch (Exception ex) {
                _logger.LogError($"Error during log file compression: {ex.Message}");
            }
        }
    }

    // Simple logger interface and implementation
    public interface ILogger {
        void LogInformation(string message);
        void LogWarning(string message);
        void LogError(string message);
    }

    public class ConsoleLogger : ILogger {
        public void LogInformation(string message) =>
            Console.WriteLine($"[INFO] {DateTime.Now}: {message}");

        public void LogWarning(string message) =>
            Console.WriteLine($"[WARNING] {DateTime.Now}: {message}");

        public void LogError(string message) =>
            Console.WriteLine($"[ERROR] {DateTime.Now}: {message}");
    }

    // Example of how to use the service
    public class Program {
        static async Task Main(string[] args) {
            string logDirectory = @"C:\Logs"; // Change to your log directory
            string logPattern = "*.log";      // Change to match your log file pattern

            var compressor = new LogRolloverCompressor(
                logDirectory: logDirectory,
                logFilePattern: logPattern,
                pollingIntervalMs: 5000  // Poll every 5 seconds
            );

            // Start the compression service
            compressor.Start();

            Console.WriteLine("Log rollover compression service started.");
            Console.WriteLine("Press any key to stop...");
            Console.ReadKey();

            // Stop the service when done
            await compressor.StopAsync();
        }
    }
}