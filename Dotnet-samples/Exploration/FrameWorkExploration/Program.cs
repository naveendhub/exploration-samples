using System;
using System.Collections.Generic;
using System.IO;

using Exploration;

namespace FrameWorkExploration {
    internal class Program {
        static void Main(string[] args) {

            var pwd = new CompressionUtility();
            pwd.Run();


            Console.ReadLine();
        }
    }
}
