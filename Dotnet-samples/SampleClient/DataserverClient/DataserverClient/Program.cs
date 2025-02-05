using Philips.Platform.ApplicationIntegration;
using Philips.Platform.Common;
using Philips.Platform.SystemIntegration.Bootstrap;
using System;

namespace DataserverClient
{
    class Program
    {
        private static string deviceId = "LocalDatabase";


        static void Main(string[] args)
        {
            BootStrap();
            
            var wadoClient = new WadoClient();
            wadoClient.Run();
            

            ExitApplication();
        }


        private static void ExitApplication() {
            Console.WriteLine("Press key to exit");
            Console.ReadLine();
            Environment.Exit(0);
        }
        private static void BootStrap() {
            
            var bootstrapPath = @"C:\Program Files\Philips\IPF\DataServer\IPF\Bin";
        
            DataServerBootstrap.Execute(bootstrapPath);
            //To test
            //var studies = QueryManager.QueryStudy(deviceId, DicomFilter.MatchAll());
        }


    }

}
