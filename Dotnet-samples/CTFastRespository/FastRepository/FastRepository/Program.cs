using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Alachisoft.NCache.Client;
using Philips.Platform.ApplicationIntegration.DataAccess;
using Philips.Platform.Common;
using Philips.Platform.CommonUtilities.AssemblyResolution;
using Philips.Platform.CommonUtilities.AssemblyResolutionFind;
using Philips.Platform.SystemIntegration;

namespace FastRepository {
    class Program {

        private static string commonUtilitiesPath = @"C:\Views\MainlineGit\Common\Output\Bin";
        private static readonly AutoResetEvent MessageEvent = new AutoResetEvent(false);
        private static FastRepositoryToolkit acquisitionToolkit;
        private static readonly object SynLock = new object();
        private static int iterationCount = 3001;

        private static long[] storeTime = new long[iterationCount];
        private static long[] ReadTime = new long[iterationCount];

        private static string logFolder = @"C:\WorkingDirectory\DataServerClientLogs";
        private static string logFileName= Process.GetCurrentProcess().Id + ".csv";

        private static readonly bool normalizedFormat = false;
         
        static void Main(string[] args)
        {

            try
            {
                BootStrapDataServer();

                RunInMultiProcessMode(args);

            }
            catch (Exception ex) {
                Console.WriteLine(ex);
            }
            Console.WriteLine("Press any key to exit");
            Console.ReadLine();

        }
        
        #region Multiprocess
        private static void RunInMultiProcessMode(string[] args) {
            var viewerMode = args != null && args[0].ToUpperInvariant() == "-VIEWER";
            var reconMode = args != null && args[0].ToUpperInvariant() == "-RECON";
            acquisitionToolkit = new FastRepositoryToolkit(iterationCount, normalizedFormat);
            WarmUp();

            if (viewerMode) {
                var subscriber = new EventSubscriber();
                subscriber.ImageReceived += OnReceivingImage;

                MessageEvent.WaitOne();

                
                DumpArray(ReadTime, "Time taken to load full header in ticks ", 
                    Path.Combine(logFolder,$"Load_FullHeaders_{logFileName}"));
                if (normalizedFormat) {
                    DumpArray(acquisitionToolkit.studyLoadTime, "Time taken to load study header in ticks",
                        Path.Combine(logFolder, $"Load_Study_{logFileName}"));
                    DumpArray(acquisitionToolkit.seriesLoadTime, "Time taken to load series header in ticks",
                        Path.Combine(logFolder, $"Load_Series_{logFileName}"));
                    DumpArray(acquisitionToolkit.imageLoadTime, "Time taken to load image header in ticks",
                        Path.Combine(logFolder, $"Load_Image_{logFileName}"));

                }


                subscriber.ImageReceived -= OnReceivingImage;

            } else if (reconMode) {

                StoreImageFromRecon();

                DumpArray(storeTime, "Time taken to store full header in ticks",
                    Path.Combine(logFolder, $"Store_FullHeaders_{logFileName}"));
                if (normalizedFormat) {
                    DumpArray(acquisitionToolkit.studyStoreTime, "Time taken to store study header in ticks",
                        Path.Combine(logFolder, $"Store_Study_{logFileName}"));
                    DumpArray(acquisitionToolkit.seriesStoreTime, "Time taken to store series header in ticks",
                        Path.Combine(logFolder, $"Store_Series_{logFileName}"));
                    DumpArray(acquisitionToolkit.imageStoreTime, "Time taken to store image header in ticks",
                        Path.Combine(logFolder, $"Store_Image_{logFileName}"));
                }

            } else {
                throw new InvalidOperationException("Run in either Recon or Viewer mode");
            }
        }

        private static void WarmUp() {
            var dicomObject = DicomObject.CreateInstance();
            dicomObject.SetString(DicomDictionary.DicomStudyDescription,"ABC");
            dicomObject.GetString(DicomDictionary.DicomStudyDescription);
            var studyHeaders = ConfigurationManager.SeriesHeaders;
            var seriesHeaders = ConfigurationManager.StudyHeaders;
        }
        
        private static void StoreImageFromRecon() {
            var publisher = new EventPublisher();
            var stopWatch = new Stopwatch();
            int imageCounter = 0;

            //Store dummy data
            var demoFile = Directory.GetFiles(@"C:\TestData\3000_CT_Body\9aba31\CT5").FirstOrDefault();
            var demoDicomObject = DicomObject.CreateInstance(demoFile);
            demoDicomObject.SetString(DicomDictionary.DicomStudyInstanceUid, Guid.NewGuid().ToString());
            demoDicomObject.SetString(DicomDictionary.DicomSeriesInstanceUid, Guid.NewGuid().ToString());
            
            stopWatch.Start();
            var demoKey = acquisitionToolkit.StoreHeader(demoDicomObject);
            stopWatch.Stop();
            storeTime[imageCounter] = stopWatch.ElapsedTicks;
            imageCounter++;
            //Publish
            publisher.Publish(demoKey);
            stopWatch.Reset();

            //Actual images
            var files = Directory.GetFiles(@"C:\TestData\3000_CT_Body\9aba31\CT5");
            foreach (var file in files)
            {
                var dicomObject = DicomObject.CreateInstance(file);

                stopWatch.Start();
                var storageKey = acquisitionToolkit.StoreHeader(dicomObject);
                stopWatch.Stop();
                storeTime[imageCounter] = stopWatch.ElapsedTicks;
                imageCounter++;
                //Publish
                publisher.Publish(storageKey);

                stopWatch.Reset();
                
            }

        }

        private static int counter = 0;

        private static void OnReceivingImage(object sender, ImageStoredEventArgs args) {
            lock (SynLock) {
                
                var message = args.Identifier.Split(',');
                var identifier = message[1];
                var studyId = message[2];
                var seriesId = message[3];
                int studyHeaderLength = string.IsNullOrEmpty(message[4])? 0: int.Parse(message[4]);
                int seriesHeaderLength = string.IsNullOrEmpty(message[5]) ? 0 : int.Parse(message[5]);
                int imageHeaderLength = string.IsNullOrEmpty(message[6]) ? 0 : int.Parse(message[6]);


                //Get headers and pixel
                var stopwatch = Stopwatch.StartNew();

                var dicomObject = acquisitionToolkit.LoadHeaders(identifier, studyId, seriesId, studyHeaderLength, seriesHeaderLength, imageHeaderLength);
                
                stopwatch.Stop();
                ReadTime[counter] = stopwatch.ElapsedTicks;

                Interlocked.Increment(ref counter);
                
                if (counter == iterationCount) {
                    MessageEvent.Set();
                }
            }
        }

        public static void DumpArray(long[] arr, string message, string fileName) {

            using (System.IO.StreamWriter txtWriter = new System.IO.StreamWriter(fileName, true)) {
                foreach (var item in arr) {
                    txtWriter.WriteLine($"{message} , {item}");
                }
            }
        }

        #endregion

        #region Private

        private static void OnReceivingMessage(object sender, ImageStoredEventArgs args) {
            lock (SynLock) {
                var message = args.Identifier.Split(',');
                var identifier = message[1];
                Console.WriteLine(
                    $"Event received key: {identifier}  Time: {System.DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss.fff tt")}");
            }
        }

        private static void OnReceivingByteData(object sender, ImageStoredEventArgs args) {
            lock (SynLock) {
                Task.Factory.StartNew(() => {
                    var message = args.Identifier.Split(',');
                    var identifier = message[1];

                    var stopWatch = Stopwatch.StartNew();
                    var pixelFromCache = acquisitionToolkit.LoadPixel(identifier);
                    stopWatch.Stop();
                    Console.WriteLine($"Time taken to get byte[] of size 2 MB is {stopWatch.ElapsedMilliseconds}");

                });
            }
        }
        private static void MeasureSerializationAndDeserialization() {

            int iterationCount = 10;

            for (int i = 0; i < iterationCount; i++) {
                var dicomObject = DicomObject.CreateInstance(@"C:\TestData\3000_CT_Body\9aba31\CT5\CT000000.dcm");
                var sopInstanceUid = DicomObject.GenerateDicomUid();
                dicomObject.SetString(DicomDictionary.DicomSopInstanceUid, sopInstanceUid);
                dicomObject.Remove(DicomDictionary.DicomPixelData);

                Console.WriteLine($"SOP before serialzation {dicomObject.GetString(DicomDictionary.DicomSopInstanceUid)}");
                //Store
                var stopWatch = Stopwatch.StartNew();

                var destStream = new MemoryStream();
                dicomObject.Serialize(destStream);
                stopWatch.Stop();
                Console.WriteLine($"Time take to serialize  {stopWatch.ElapsedMilliseconds}");


                destStream.Seek(0, SeekOrigin.Begin);
                stopWatch.Restart();
                var result = DicomObject.CreateInstance(destStream);
                stopWatch.Stop();

                Console.WriteLine($"Time take to de-serialize  {stopWatch.ElapsedMilliseconds} sopAfterSerialization {result.GetString(DicomDictionary.DicomSopInstanceUid)}");

            }

        }
        private static void AddByteDataToCache() {
            int iterationCount = 100;
            var publisher = new EventPublisher();
            const int pixelSizeInKb = 1024 * 2;
            var pixelData = GetByteArray(pixelSizeInKb);

            for (int i = 0; i < iterationCount; i++) {
                //Store
                var key = $"PatientId:PatientName:{DicomObject.GenerateDicomUid()}";
                var stopWatch = Stopwatch.StartNew();
                acquisitionToolkit.StorePixel(key, pixelData);
                stopWatch.Stop();
                Console.WriteLine($"Iteration {i + 1}. Time taken to store byte[] of size {pixelSizeInKb} kb/ {pixelSizeInKb / 1024} is {stopWatch.ElapsedMilliseconds}");
                //Publish
                publisher.Publish(key);

                Thread.Sleep(15); //This ensures that around 70 messages are sent in a second.(70 frame/seconds of CT requirement)
            }

        }
        private static void RunInSameProcess() {
            acquisitionToolkit = new FastRepositoryToolkit(iterationCount, true);
            WarmUp();
            EvaluateStoreAndFetch();

            //const int iterationCount = 5;
            //for (int i = 0; i < iterationCount; i++)
            //{
            //    EvaluateStoreAndFetch();
            //}
        }

        private static void EvaluateStoreAndFetch() {
            var dicomObject = DicomObject.CreateInstance(@"C:\TestData\3000_CT_Body\9aba31\CT5\CT000000.dcm");
            var sopInstanceUid = DicomObject.GenerateDicomUid();
            dicomObject.SetString(DicomDictionary.DicomSopInstanceUid, sopInstanceUid);
            var pixelSizeInKb = 1024 * 2;
            var pixelData = GetByteArray(pixelSizeInKb);

            var overAllTime = Stopwatch.StartNew();
            var stopWatch = Stopwatch.StartNew();

            //Store
            var key = acquisitionToolkit.StoreHeader(dicomObject);
            acquisitionToolkit.StorePixel(key, pixelData);

            stopWatch.Stop();

            Console.WriteLine(
                $"TimeTaken to add DicomObject sopUid {sopInstanceUid} with pixel size" +
                $" {pixelSizeInKb} kB/{pixelSizeInKb / 1024} MB is {stopWatch.ElapsedMilliseconds} ms");

            stopWatch.Restart();

            //Load
            var dicomObjectFromCache = acquisitionToolkit.LoadHeaders(key, string.Empty, String.Empty, 0, 0, 0);
            var pixelFromCache = acquisitionToolkit.LoadPixel(key);

            stopWatch.Stop();
            overAllTime.Stop();

            Console.WriteLine(
                $"TimeTaken to Get DicomObject sopUid {dicomObjectFromCache.GetString(DicomDictionary.DicomSopInstanceUid)}" +
                $" with pixel size {pixelSizeInKb} kB/{pixelSizeInKb / 1024} MB is {stopWatch.ElapsedMilliseconds} ms");
            Console.WriteLine(
                $"TimeTaken to Store/Load DicomObject with pixel size" +
                $" {pixelSizeInKb} kB/{pixelSizeInKb / 1024} MB is {overAllTime.ElapsedMilliseconds} ms");

            //acquisitionToolkit.RemoveData(key);
        }
        private static void TestCaching(ICache cache) {
            int[] byteSizeInKb =
            {
                10, 20, 50, 100, 500, 1024, 1024 * 2, 1024 * 5, 1024 * 10, 1024 * 20, 1024 * 50, 1024 * 100, 1024 * 500,
                1024 * 750
            };

            foreach (var byteSize in byteSizeInKb) {
                string key = $"PatientId1:PatientName1:StudyId1:SeriesId1:SopInstanceId1:{byteSize}";
                //int byteSizeKb = 1024 * byteSize;

                var pixelData = GetByteArray(byteSize);

                var stopWatch = Stopwatch.StartNew();

                var pixelCacheItem = new CacheItem(pixelData);

                cache.Add(key, pixelCacheItem);

                var dataFromCache = cache.Get<byte[]>(key);

                stopWatch.Stop();

                Console.WriteLine(
                    $"TimeTaken to add/get byte data of size {byteSize} KB/ {(float)byteSize / 1024} MB is {stopWatch.ElapsedMilliseconds} ms");

                cache.Remove(key);
            }
        }

        private static byte[] GetByteArray(int sizeInKb) {
            Random rnd = new Random();
            byte[] b = new byte[sizeInKb * 1024]; // convert kb to byte
            rnd.NextBytes(b);
            return b;
        }
        private static void PublishEvents(int iterationCount) {
            var publisher = new EventPublisher();
            for (int i = 0; i < iterationCount; i++) {
                var key =
                    $"Jhon^Kenady^SamplePrefix^SampleSuffix" +
                    $"{DicomObject.GenerateDicomUid()}_{DicomObject.GenerateDicomUid()}_{i}_{System.DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss.fff tt")}";
                publisher.Publish(key);
                Thread.Sleep(15); //This ensures that around 70 messages are sent in a second.(70 frame/seconds of CT requirement)
            }
        }


        #endregion
        
        #region customLog

        static string logFile = @"C:\WorkDirectory\DataServerClientLogs\MyLog_" + Process.GetCurrentProcess().Id + ".txt";
        private static readonly object customLogLock = new object();

        internal static void Trace(string message) {
            Console.WriteLine(message);
            CustomLog(message);
        }
        internal static void CustomLog(string logMessage) {
            lock (customLogLock) {
                using (System.IO.StreamWriter txtWriter =
                    new System.IO.StreamWriter(logFile, true)) {
                    txtWriter.WriteLine("{0} : PID {1} : ThreadID {2} : {3}",
                        System.DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss.fff tt"),
                        Process.GetCurrentProcess().Id,
                        Thread.CurrentThread.ManagedThreadId,
                        logMessage);
                }
            }
        }
        
        #endregion
        
        #region DataServerBootstrap

        private static void BootStrapDataServer() {
            AssemblyResolverFind();
            StartAssemblyResolver();
            Bootstrap();

        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void AssemblyResolverFind() {
            AssemblyResolverFinder.AddSearchPath(commonUtilitiesPath);
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

        #endregion

        #region commented
        //private static void LoadImageInViewer(IAcquisitionToolkit acquisitionToolkit) {
        //    const int processRetryInterval = 50;
        //    while (true) {
        //        if (
        //            !messageReadyEvent.WaitOne(
        //                TimeSpan.FromMilliseconds(processRetryInterval))
        //        ) {
        //            continue;
        //        }
        //        messageReadyEvent.Reset();
        //        var dicomObjectFromCache = acquisitionToolkit.LoadHeaders(identifier);
        //        var pixelFromCache = acquisitionToolkit.LoadPixel(identifier);
        //        Trace($"Data loaded for {identifier}");
        //    }
        //}

        //private static void OnReceivingImage(object sender, ImageStoredEventArgs args) {
        //    lock (SynLock) {
        //        //Task.Factory.StartNew(() => {
        //        var message = args.Identifier.Split(',');
        //        var identifier = message[1];
        //        //Get headers and pixel

        //        acquisitionToolkit.LoadHeaders(identifier);
        //        acquisitionToolkit.LoadPixel(identifier);
        //        //var currentTime = DateTime.Now;
        //        //DateTime.TryParseExact(identifier, "dd/MM/yyyy hh:mm:ss.fff tt", null, DateTimeStyles.None, out var storeDate);
        //        //var differenceTime = currentTime.Subtract(storeDate);
        //        ////Console.WriteLine($"Data loaded RecievedTimeStamp: {System.DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss.fff tt")} and Key: {identifier} . TimeToLoad: {stopWatch.ElapsedMilliseconds}  ");
        //        //Console.WriteLine($"Time taken for store and load:  {differenceTime.TotalMilliseconds}");
        //        //});

        //    }
        //}
        #endregion
    }
}
