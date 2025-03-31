using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrameWorkExploration {
    public static class LogSyncConstants {
        // Convention: "Global\DirectoryName_LogSync"
        public static string GetEventName() {
            return $"Global\\Test_LogSync";
        }

        // Convention: "Global\DirectoryName_LogLock"
        public static string GetLockName(string directoryPath) {
            string dirName = Path.GetFileName(directoryPath);
            return $"Global\\{dirName}_LogLock";
        }

        // Maximum wait time in milliseconds before assuming Process A has crashed
        public const int MaxWaitTimeMs = 60000; // 1 minute timeout

        // Heartbeat interval in milliseconds
        public const int HeartbeatIntervalMs = 5000; // 5 seconds

        // File indicating directory is in processing by Process A
        public static string GetProcessingFlagFile(string directoryPath) {
            return Path.Combine(directoryPath, ".processing");
        }
    }
}
