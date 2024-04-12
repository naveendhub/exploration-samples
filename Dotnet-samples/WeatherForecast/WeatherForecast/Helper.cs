using System.Diagnostics;

namespace WeatherForecast
{
    public static class Helper
    {

        static string logFile = @"C:\Workspace\Logs\MyLog_" + Process.GetCurrentProcess().Id + ".txt";
        private static readonly object customLogLock = new object();

        internal static void CustomLog(string logMessage)
        {
            lock (customLogLock)
            {
                using (System.IO.StreamWriter txtWriter =
                       new System.IO.StreamWriter(logFile, true))
                {
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
