using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FrameWorkExploration.ProcessSynchronization {
    public class LogCompressionService {

        public static void Run() {
            string[] directories = { @"C:\Logs\Category1", @"C:\Logs\Category2", @"C:\Logs\Category3" };

            // We'll use a non-blocking approach to process directories as they become available
            ProcessDirectoriesNonBlocking(directories);
        }

        private static void ProcessDirectoriesNonBlocking(string[] directories) {
            // Create a dictionary to hold event handles for each directory
            var eventHandles = new Dictionary<string, EventWaitHandle>();
            var pendingDirectories = new HashSet<string>(directories);
            var dirStartTimes = new Dictionary<string, DateTime>();

            // Open or create all event handles
            foreach (string directory in directories) {
                string eventName = LogSyncConstants.GetEventName(directory);

                try {
                    // Try to open existing EventWaitHandle (created by Process A)
                    eventHandles[directory] = EventWaitHandle.OpenExisting(eventName);
                    dirStartTimes[directory] = DateTime.UtcNow;
                    Console.WriteLine($"Process B: Found existing event handle for {directory}");
                } catch (WaitHandleCannotBeOpenedException) {
                    // Process A hasn't created this handle yet, create it as signaled (available)
                    eventHandles[directory] = new EventWaitHandle(true, EventResetMode.ManualReset, eventName);
                    dirStartTimes[directory] = DateTime.UtcNow;
                    Console.WriteLine($"Process B: Created new event handle for {directory}");
                }
            }

            try {
                // Continue until all directories are processed
                while (pendingDirectories.Count > 0) {
                    foreach (string directory in pendingDirectories.ToList()) {
                        bool canProcess = false;
                        string processingFlagFile = LogSyncConstants.GetProcessingFlagFile(directory);

                        // Check if the directory is being processed by Process A
                        bool isBeingProcessed = File.Exists(processingFlagFile);

                        if (isBeingProcessed) {
                            // Check if the event is signaled (normal case - Process A finished)
                            if (eventHandles[directory].WaitOne(0)) {
                                Console.WriteLine($"Process B: Directory {directory} signaled as available");
                                canProcess = true;
                            } else {
                                // Check for timeout or crashed Process A
                                TimeSpan waitTime = DateTime.UtcNow - dirStartTimes[directory];

                                // First check heartbeat file
                                string heartbeatFile = Path.Combine(directory, ".heartbeat");
                                if (File.Exists(heartbeatFile)) {
                                    try {
                                        string timestampStr = File.ReadAllText(heartbeatFile);
                                        if (DateTime.TryParse(timestampStr, out DateTime lastHeartbeat)) {
                                            TimeSpan heartbeatAge = DateTime.UtcNow - lastHeartbeat;
                                            if (heartbeatAge.TotalMilliseconds >
                                                LogSyncConstants.HeartbeatIntervalMs * 3) {
                                                Console.WriteLine(
                                                    $"Process B: Detected stale heartbeat for {directory}, " +
                                                    $"last update was {heartbeatAge.TotalSeconds:F1} seconds ago");
                                                canProcess = true;
                                            }
                                        }
                                    } catch (Exception ex) {
                                        Console.WriteLine($"Process B: Error reading heartbeat file: {ex.Message}");
                                    }
                                }
                                // If no heartbeat file or can't read it, check timeout
                                else if (waitTime.TotalMilliseconds > LogSyncConstants.MaxWaitTimeMs) {
                                    Console.WriteLine($"Process B: Timeout waiting for directory {directory}, " +
                                                      $"waited {waitTime.TotalSeconds:F1} seconds");
                                    canProcess = true;
                                }
                            }
                        } else {
                            // Directory is not being processed by Process A, we can process it
                            Console.WriteLine($"Process B: Directory {directory} is not being processed by any reader");
                            canProcess = true;
                        }

                        if (canProcess) {
                            try {
                                // Try to acquire the mutex to ensure Process A is not still working
                                string lockName = LogSyncConstants.GetLockName(directory);
                                Mutex directoryLock = null;
                                bool mutexExists = true;

                                try {
                                    directoryLock = Mutex.OpenExisting(lockName);
                                } catch (WaitHandleCannotBeOpenedException) {
                                    // Mutex doesn't exist, create it
                                    mutexExists = false;
                                    directoryLock = new Mutex(false, lockName);
                                }

                                bool acquired = directoryLock.WaitOne(0);
                                if (acquired) {
                                    // We got the lock, Process A must be done or crashed or never ran
                                    Console.WriteLine($"Process B: Starting compression for directory {directory}");

                                    // Clean up any leftover processing flag or heartbeat files
                                    CleanupProcessAFiles(directory);

                                    // Compress logs
                                    CompressLogsInDirectory(directory);

                                    // Release the lock
                                    directoryLock.ReleaseMutex();
                                    directoryLock.Dispose();

                                    // Remove from pending list and reset event
                                    pendingDirectories.Remove(directory);
                                    eventHandles[directory].Reset();
                                } else {
                                    Console.WriteLine(
                                        $"Process B: Directory {directory} appears available but lock is held, " +
                                        "will check again later");
                                    directoryLock.Dispose();
                                }
                            } catch (Exception ex) {
                                Console.WriteLine($"Process B: Error acquiring lock for {directory}: {ex.Message}");
                            }
                        }
                    }

                    // If we still have pending directories, wait a bit before checking again
                    if (pendingDirectories.Count > 0) {
                        Console.WriteLine(
                            $"Process B: Waiting for available directories. Pending: {string.Join(", ", pendingDirectories)}");
                        Thread.Sleep(100);
                    }
                }
            } finally {
                // Clean up all event handles
                foreach (var handle in eventHandles.Values) {
                    handle.Dispose();
                }
            }
        }

        private static void CleanupProcessAFiles(string directory) {
            try {
                string heartbeatFile = Path.Combine(directory, ".heartbeat");
                string processingFlagFile = LogSyncConstants.GetProcessingFlagFile(directory);

                if (File.Exists(heartbeatFile)) {
                    File.Delete(heartbeatFile);
                }

                if (File.Exists(processingFlagFile)) {
                    File.Delete(processingFlagFile);
                }
            } catch (Exception ex) {
                Console.WriteLine($"Process B: Error cleaning up Process A files: {ex.Message}");
            }
        }

        private static void CompressLogsInDirectory(string directoryPath) {
            // Process log files in the directory
            foreach (string logFile in Directory.GetFiles(directoryPath, "*.csv")) {
                Console.WriteLine($"Process B: Compressing log file {logFile}");

                // Simulate log compression
                Thread.Sleep(300);
            }

            Console.WriteLine($"Process B: Finished compressing logs in {directoryPath}");
        }
    }
}
