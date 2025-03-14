namespace Exploration {
    public class LogProcesssor {
        public void Run() {
            string[] directories = { @"C:\Logs\Category1", @"C:\Logs\Category2" };
            
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
        private bool _disposed;
        
        public LogDirectoryWrapper(string directoryPath) {
            _directoryPath = directoryPath;
            
            try {
                // Create or open a named event for this directory
                string eventName = LogSyncConstants.GetEventName(directoryPath);

                // Initial state: non-signaled (false), meaning the directory is in use
                _eventHandle = new EventWaitHandle(false, EventResetMode.ManualReset, eventName);

                _eventHandle.Reset();
                Console.WriteLine($"Process A: Started processing directory {directoryPath}");
            } catch (Exception ex) {
                Console.WriteLine($"Process A: Error initializing directory wrapper: {ex.Message}");
                // Clean up any resources we might have acquired
                Dispose(true);
                throw;
            }
        }
        
        public void ProcessLogs() {
            // Simulate log processing
            Thread.Sleep(TimeSpan.FromSeconds(1));
            Console.WriteLine($"Process A: Processed log files");
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {

                    // Signal that this directory is no longer in use
                    _eventHandle?.Set();
                    Console.WriteLine($"Process A: Finished processing directory {_directoryPath}");
                    // Clean up handles
                    _eventHandle?.Dispose();
                }

                _disposed = true;
            }
        }

        ~LogDirectoryWrapper() {
            Dispose(false);
        }
    }

}
