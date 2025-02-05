using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Philips.Platform.ApplicationIntegration;
using Philips.Platform.CommonUtilities.Logging;
using Philips.Platform.SystemIntegration.Bootstrap;
using DevelopmentLogger = Philips.Platform.ApplicationIntegration.Log.DevelopmentLogger;
using InfoCategory = Philips.Platform.ApplicationIntegration.Log.InfoCategory;
//using DevelopmentLogger = Philips.Platform.CommonUtilities.Logging.DevelopmentLogger;
//using InfoCategory = Philips.Platform.CommonUtilities.Logging.InfoCategory;
using Logger = Philips.Platform.CommonUtilities.Logging.Logger;

namespace DataserverClient {
    class Program {
        
        private static readonly Random random = new Random();
        private static string deviceId = "MRDicomRepository";
        private static int iteration = 10000;
        private static int pid;
        static void Main(string[] args) {
            pid = GetPID();
            logFile = @"C:\MyLog_" + pid + ".txt";
            BootStrap();
            DevelopmentLogger logger = new DevelopmentLogger("test", "na");
            logger.Log("bac", Severity.Error, InfoCategory.None);
            Logger.Log();




            ExitApplication();
        }

        private static string logFile;
        private static readonly object customLogLock = new object();

        internal static void CustomLog(string logMessage) {
            lock (customLogLock) {
                using (System.IO.StreamWriter txtWriter =
                    new System.IO.StreamWriter(logFile, true)) {
                    txtWriter.WriteLine("{0} : PID {1} : ThreadID {2} : {3}",
                        System.DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss.fff tt"),
                        pid,
                        Thread.CurrentThread.ManagedThreadId,
                        logMessage);
                }
            }
        }

        private static void ExitApplication() {
            Console.WriteLine("Press key to exit");
            Console.ReadLine();
            Environment.Exit(0);
        }
        private static void BootStrap() {
            var bootstrapPath = Directory.GetCurrentDirectory();
            Trace("BootstapPath : " + bootstrapPath);
            Trace("deviceID : " + deviceId);
            DataServerBootstrap.Execute(bootstrapPath);
        }

        private static void Trace(string message) {
            Console.WriteLine(message);
            CustomLog(message);
        }

        

        private static int GetPID() {
            return Process.GetCurrentProcess().Id;
        }

    }

}
