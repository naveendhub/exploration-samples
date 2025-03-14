namespace Exploration.ProcessSynchronization {
    internal class LogProcesssor {
        public static void Run() {
            string[] directories = { @"C:\Logs\Category1", @"C:\Logs\Category2", @"C:\Logs\Category3" };

            AppDomain.CurrentDomain.ProcessExit += (sender, e) => {
                Console.WriteLine("Process A: Shutting down gracefully...");
                // Allow time for cleanup
                Thread.Sleep(1000);
            };

            foreach (string directory in directories) {
                try {
                    using (var logWrapper = new LogDirectoryWrapper(directory)) {
                        logWrapper.ProcessLogs();
                        // LogDirectoryWrapper.Dispose() will be called automatically
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"Process A: Error processing directory {directory}: {ex.Message}");
                }
            }
        }
    }

    public class LogDirectoryWrapper : IDisposable {
        private readonly string _directoryPath;
        private readonly EventWaitHandle _eventHandle;
        private readonly Mutex _directoryLock;
        private readonly Timer _heartbeatTimer;
        private readonly string _processingFlagFile;
        private bool _disposed = false;
        private DateTime _lastModifiedTime;

        public LogDirectoryWrapper(string directoryPath) {
            _directoryPath = directoryPath;
            _processingFlagFile = LogSyncConstants.GetProcessingFlagFile(directoryPath);

            try {
                // Create or open a named event for this directory
                string eventName = LogSyncConstants.GetEventName(directoryPath);

                // Initial state: non-signaled (false), meaning the directory is in use
                _eventHandle = new EventWaitHandle(false, EventResetMode.ManualReset, eventName);

                // Create or open a mutex for directory locking
                string lockName = LogSyncConstants.GetLockName(directoryPath);
                _directoryLock = new Mutex(true, lockName, out bool createdNew);

                if (!createdNew) {
                    // Another process already has the lock, wait for it
                    Console.WriteLine($"Process A: Waiting for lock on directory {directoryPath}");
                    _directoryLock.WaitOne();
                }

                // Create a processing flag file to indicate directory is being processed
                File.WriteAllText(_processingFlagFile, DateTime.UtcNow.ToString("o"));

                // Create a timestamp file to track when we started processing
                _lastModifiedTime = DateTime.UtcNow;
                UpdateHeartbeatFile();

                // Set up a heartbeat timer to update the timestamp regularly
                _heartbeatTimer = new Timer(HeartbeatCallback, null, 0, LogSyncConstants.HeartbeatIntervalMs);

                Console.WriteLine($"Process A: Started processing directory {directoryPath}");
            } catch (Exception ex) {
                Console.WriteLine($"Process A: Error initializing directory wrapper: {ex.Message}");
                // Clean up any resources we might have acquired
                Dispose(true);
                throw;
            }
        }

        private void HeartbeatCallback(object state) {
            UpdateHeartbeatFile();
        }

        private void UpdateHeartbeatFile() {
            try {
                string heartbeatFile = Path.Combine(_directoryPath, ".heartbeat");
                _lastModifiedTime = DateTime.UtcNow;
                File.WriteAllText(heartbeatFile, _lastModifiedTime.ToString("o"));
            } catch (Exception ex) {
                Console.WriteLine($"Process A: Error updating heartbeat: {ex.Message}");
            }
        }

        public void ProcessLogs() {
            // Process log files in the directory
            foreach (string logFile in Directory.GetFiles(_directoryPath, "*.csv")) {
                Console.WriteLine($"Process A: Processing log file {logFile}");

                // Simulate log processing
                Thread.Sleep(500);

                // Update heartbeat after each file
                UpdateHeartbeatFile();
            }
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    // Stop the heartbeat timer
                    _heartbeatTimer?.Dispose();

                    // Signal that this directory is no longer in use
                    _eventHandle?.Set();
                    Console.WriteLine($"Process A: Finished processing directory {_directoryPath}");

                    // Release the mutex lock
                    try {
                        _directoryLock?.ReleaseMutex();
                    } catch (ApplicationException) {
                        // Mutex might not be owned by this thread if we failed during initialization
                    }

                    // Remove the heartbeat and processing flag files
                    try {
                        string heartbeatFile = Path.Combine(_directoryPath, ".heartbeat");
                        if (File.Exists(heartbeatFile)) {
                            File.Delete(heartbeatFile);
                        }

                        if (File.Exists(_processingFlagFile)) {
                            File.Delete(_processingFlagFile);
                        }
                    } catch (Exception ex) {
                        Console.WriteLine($"Process A: Error removing heartbeat/flag files: {ex.Message}");
                    }

                    // Clean up handles
                    _eventHandle?.Dispose();
                    _directoryLock?.Dispose();
                }

                _disposed = true;
            }
        }

        ~LogDirectoryWrapper() {
            Dispose(false);
        }
    }

}
