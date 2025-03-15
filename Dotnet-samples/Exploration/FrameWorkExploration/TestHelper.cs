using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FrameWorkExploration {
    internal static class TestHelper {

        static string logFile = @"C:\MyLog_" + Process.GetCurrentProcess().Id + ".txt";

        private static readonly object customLogLock = new object();

        internal static void CustomLog(string logMessage) {

            lock (customLogLock) {

                using (System.IO.StreamWriter txtWriter =

                       new System.IO.StreamWriter(logFile, true)) {

                    txtWriter.WriteLine("{0} : PID {1} : ThreadID {2} : {3}",

                        System.DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss.fff tt"),

                        Process.GetCurrentProcess().Id,

                        Thread.CurrentThread.ManagedThreadId,

                        logMessage);

                }

            }

        }
    }
}
