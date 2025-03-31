using System;
using System.Diagnostics;
using System.IO;

namespace FrameWorkExploration {
    internal class Program {
        static void Main(string[] args)
        {
            //var service = new LogCompressionService();
            //service.Run();
            var stopWatch = Stopwatch.StartNew();
            File.Delete(@"C:\Workspace\demo.txt");
            stopWatch.Stop();
            Console.WriteLine(stopWatch.ElapsedMilliseconds);

            Console.ReadLine();
        }
    }
}
