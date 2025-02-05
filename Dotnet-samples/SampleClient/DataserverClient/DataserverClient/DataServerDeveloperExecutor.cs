using System.Runtime.CompilerServices;

using Philips.Platform.ApplicationIntegration;
using Philips.Platform.Common;
using Philips.Platform.CommonUtilities.AssemblyResolution;
using Philips.Platform.CommonUtilities.AssemblyResolutionFind;
using Philips.Platform.SystemIntegration;

namespace DataserverClient {
    internal class DataServerDeveloperExecutor {
        
        private static string commonUtilitiesPath = @"C:\Views\cp\Common\Output\Bin";
        private static string commonDeploymentPath = @"C:\Views\cp\System\SystemComponents\Output\Bin";
        private string deviceId = "LocalDatabase";

        internal void Run()
        {
            BootStrapDataServer();

        }
        private void BootStrapDataServer() {
            AssemblyResolverFind();
            StartAssemblyResolver();
            Bootstrap();

            var studies = QueryManager.QueryStudy(deviceId, DicomFilter.MatchAll());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void AssemblyResolverFind() {
            AssemblyResolverFinder.AddSearchPath(commonUtilitiesPath);
            AssemblyResolverFinder.AddSearchPath(commonDeploymentPath);
            AssemblyResolverFinder.Find();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Bootstrap() {
            SystemBootstrap.Execute();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void StartAssemblyResolver() {
            AssemblyResolver.Start();
        }
    }
}
