using Philips.Platform.ApplicationIntegration;
using Philips.Platform.Common;
using Philips.Platform.SystemIntegration.Bootstrap;

namespace DataserverClient {
    internal class DataServerExecutor {

        private static string deviceId = "LocalDatabase";
        internal void Run()
        {
            Bootstrap();
            //To test
            var studies = QueryManager.QueryStudy(deviceId, DicomFilter.MatchAll());
        }
        
        private void Bootstrap() {
            var bootstrapPath = @"C:\Program Files\Philips\IPF\DataServer\IPF\Bin";

            DataServerBootstrap.Execute(bootstrapPath);
        }
        
    }
}
