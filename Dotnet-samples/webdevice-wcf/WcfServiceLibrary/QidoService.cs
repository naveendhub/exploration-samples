using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using Philips.Platform.ApplicationIntegration;
using Philips.Platform.ApplicationIntegration.DataAccess;
using Philips.Platform.Common;
using Philips.Platform.CommonUtilities.AssemblyResolution;
using Philips.Platform.CommonUtilities.AssemblyResolutionFind;
using Philips.Platform.SystemIntegration;

namespace WcfServiceLibrary
{
    public class QidoService : IQidoService
    {
        private static string commonUtilitiesPath = @"C:\Views\Clinical-Platform\Common\Output\Bin";
        private readonly string deviceId = "dss-us-east";
        public string GetData()
        {
            BootStrapDataServer();

            var stopWatch = Stopwatch.StartNew();
            //var studies = QueryManager.QueryStudy(deviceId, 
            //    DicomFilter.MatchExact(DicomDictionary.DicomStudyInstanceUid, "1.3.46.670589.54.2.10566225832108060749.27551065614192521480"));


            var studies = QueryManager.QueryStudy(deviceId,DicomFilter.MatchAll());
            
            stopWatch.Stop();

            var studyIdentifier = Identifier.CreateStudyIdentifier(Identifier.CreateDummyPatientKey(),
                "1.3.46.670589.54.2.10566225832108060749.27551065614192521480");
            var studyStorageKey = new StorageKey(deviceId, studyIdentifier);

            var seriesStopWatch = Stopwatch.StartNew();
            var series = QueryManager.QueryChildren(studyStorageKey);
            seriesStopWatch.Stop();

            return $"Number of studies {studies.Count} and children {series.Count}. " +
                   $"Time taken for study query {stopWatch.ElapsedMilliseconds} ms and series query {seriesStopWatch.ElapsedMilliseconds} ms";

        }


        #region DataServerBootstrap

        private static void BootStrapDataServer()
        {
            AssemblyResolverFind();
            StartAssemblyResolver();
            Bootstrap();

        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void AssemblyResolverFind()
        {
            AssemblyResolverFinder.AddSearchPath(commonUtilitiesPath);
            AssemblyResolverFinder.Find();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Bootstrap()
        {
            SystemBootstrap.Execute();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void StartAssemblyResolver()
        {
            AssemblyResolver.Start();
        }

        #endregion
    }
}
