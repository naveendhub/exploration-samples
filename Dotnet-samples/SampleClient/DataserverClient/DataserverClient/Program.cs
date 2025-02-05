
using System;

namespace DataserverClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var executor = new DataServerDeveloperExecutor();
            executor.Run();

            ExitApplication();
        }


        private static void ExitApplication() {
            Console.WriteLine("Press key to exit");
            Console.ReadLine();
            Environment.Exit(0);
        }
    }

}
