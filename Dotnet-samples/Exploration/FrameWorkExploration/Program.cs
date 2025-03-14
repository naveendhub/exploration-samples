using System;

namespace FrameWorkExploration {
    internal class Program {
        static void Main(string[] args)
        {
            var service = new LogCompressionService();
            service.Run();
            
            
            Console.ReadLine();
        }
    }
}
