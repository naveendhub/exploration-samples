using System;

namespace FrameWorkExploration {
    internal class Program {
        static void Main(string[] args)
        {

            var service = new TextEncoding();
            service.Run();

            Console.ReadLine();
        }
    }
}
