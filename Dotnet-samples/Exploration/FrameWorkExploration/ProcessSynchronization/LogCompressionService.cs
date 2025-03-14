using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace FrameWorkExploration {
    public class LogCompressionService {
        private Dictionary<string, DateTime> dirStartTimes = new Dictionary<string, DateTime>();

        public void Run() {
            string[] directories = { @"C:\Logs\Category1", @"C:\Logs\Category2" };

            // We'll use a non-blocking approach to process directories as they become available
            var eventHandles = new Dictionary<string, EventWaitHandle>();
            // Open or create all event handles
            foreach (string directory in directories) {
                string eventName = LogSyncConstants.GetEventName(directory);

                try {
                    // Try to open existing EventWaitHandle (created by Process A)
                    eventHandles[directory] = EventWaitHandle.OpenExisting(eventName);
                    Console.WriteLine($"Process B: Found existing event handle for {directory}");
                } catch (WaitHandleCannotBeOpenedException) {
                    // Process A hasn't created this handle yet, create it as signaled (available)
                    eventHandles[directory] = new EventWaitHandle(true, EventResetMode.ManualReset, eventName);
                    Console.WriteLine($"Process B: Created new event handle for {directory}");
                }
            }

            while (true) {
                ProcessDirectoriesNonBlocking(directories, eventHandles);
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            // Clean up all event handles
            //foreach (var handle in eventHandles.Values) {
            //    handle.Dispose();
            //}
        }

        private void ProcessDirectoriesNonBlocking(string[] directories, Dictionary<string, EventWaitHandle> eventHandles) {
            // Continue until all directories are processed
            foreach (string directory in directories) {
                bool canProcess = false;

                // Check if the event is signaled (normal case - Process A finished)
                if (eventHandles[directory].WaitOne(0)) {
                    Console.WriteLine($"Process B: Directory {directory} signaled as available");
                    if (dirStartTimes.ContainsKey(directory)) {
                        dirStartTimes.Remove(directory);
                    }
                    canProcess = true;
                } else {
                    if (!dirStartTimes.ContainsKey(directory)) {
                        dirStartTimes[directory] = DateTime.UtcNow;
                    }
                    
                    // Check for timeout or crashed Process A
                    TimeSpan waitTime = DateTime.UtcNow - dirStartTimes[directory];

                    //check timeout
                    if (waitTime.TotalMilliseconds > LogSyncConstants.MaxWaitTimeMs) {
                        Console.WriteLine($"Process B: Timeout waiting for directory {directory}, " +
                                          $"waited {waitTime.TotalSeconds:F1} seconds");
                        dirStartTimes.Remove(directory);
                        canProcess = true;
                        eventHandles[directory].Set();
                    }
                }

                if (!canProcess) {
                    continue;
                }

                try {
                    Console.WriteLine($"Process B: Starting compression for directory {directory}");

                    // Compress logs
                    CompressLogsInDirectory(directory);



                } catch (Exception ex) {
                    Console.WriteLine($"Process B: Error acquiring lock for {directory}: {ex.Message}");
                }
            }
        }

        private static void CompressLogsInDirectory(string directoryPath) {
            // Simulate log compression
            Thread.Sleep(300);
            Console.WriteLine($"Process B: Finished compressing logs in {directoryPath}");
        }
    }
}
