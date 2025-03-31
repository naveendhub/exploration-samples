using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace FrameWorkExploration {
    public class LogCompressionService {
        private Dictionary<string, DateTime> dirStartTimes = new Dictionary<string, DateTime>();

        public void Run() {
            string[] directories = { @"C:\Logs\Category1", @"C:\Logs\Category2" };


            // Open or create all event handles

            string eventName = LogSyncConstants.GetEventName();
            EventWaitHandle eventWaitHandle;
            try {
                // Try to open existing EventWaitHandle (created by Process A)
                eventWaitHandle = EventWaitHandle.OpenExisting(eventName);
                Console.WriteLine($"Process B: Found existing event handle");
            } catch (WaitHandleCannotBeOpenedException) {
                // Process A hasn't created this handle yet, create it as signaled (available)
                eventWaitHandle = new EventWaitHandle(true, EventResetMode.ManualReset, eventName);
                Console.WriteLine($"Process B: Created new event handle");
            }


            while (true) {
                ProcessDirectoriesNonBlocking(directories, eventWaitHandle);
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            // Clean up all event handles
            //foreach (var handle in eventHandles.Values) {
            //    handle.Dispose();
            //}
        }

        private void ProcessDirectoriesNonBlocking(string[] directories, EventWaitHandle eventHandle) {
            // Continue until all directories are processed
            foreach (string directory in directories) {
                // Check if the event is signaled (normal case - Process A finished)
                eventHandle.WaitOne(LogSyncConstants.MaxWaitTimeMs);
                                
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
