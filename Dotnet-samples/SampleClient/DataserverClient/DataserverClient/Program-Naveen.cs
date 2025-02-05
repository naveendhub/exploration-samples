using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Philips.Platform.ApplicationIntegration;
using Philips.Platform.ApplicationIntegration.DataAccess;
using Philips.Platform.Common;
using Philips.Platform.Common.DataAccess;
using Philips.Platform.SystemIntegration;
using Philips.Platform.SystemIntegration.Bootstrap;

namespace DataserverClient {
    class ImportSourceInfo
    {
        public byte[] beforeImportByteArray;
        public string sourceName;
        public StorageKey storgeKey;
    }
    class Program {
        //private static readonly AutoResetEvent storeCompletedEvent = new AutoResetEvent(false);
        //private static int _counter = 0;
        private static readonly object objLock = new object();
        static void Main(string[] args)
        {
            var bootstrapPath = @"C:\Program Files (x86)\PMS\IPF\DataServer\IPF\Bin";

            //var bootstrapPath = @"C:\Program Files\Philips\IPF\DataServer\IPF\Bin";
            DataServerBootstrap.Execute(bootstrapPath);

            DictionaryTag PixelDataAsBytesTag =
                new DictionaryTag(
                    DicomDictionary.DicomPixelData.Tag,
                    DicomVR.OW,
                    DicomDictionary.DicomPixelData.ValueMultiplicity,
                    DicomDictionary.DicomPixelData.Name,
                    DicomDictionary.DicomPixelData.ImplementerId);
            
            IList<string> files = Directory.GetFiles(@"I:\Testdata\geom_images");
            //IList<string> files = Directory.GetFiles(@"C:\TestData\PFIssue\SPT_GEOM\geom_images");
            List<StorageKey> storageKeyCollection = new List<StorageKey>();
            CustomLog("Preparing source data");
            IDictionary<string, ImportSourceInfo> beforeImport = new Dictionary<string, ImportSourceInfo>();
            foreach (var soruceFile in files)
            {
                if (soruceFile.EndsWith("_IM.dcm"))
                {
                    DicomObject dicomObjectBeforeImport = DicomObject.CreateInstance(soruceFile);
                    
                    string sopClassid = dicomObjectBeforeImport.GetString(DicomDictionary.DicomSopClassUid);

                    if (sopClassid == "1.2.840.10008.5.1.4.1.1.4")
                    {
                        string sopInstanceid = dicomObjectBeforeImport.GetString(DicomDictionary.DicomSopInstanceUid);
                        string studyInstanceId =
                            dicomObjectBeforeImport.GetString(DicomDictionary.DicomStudyInstanceUid);
                        string seriesInstanceId =
                            dicomObjectBeforeImport.GetString(DicomDictionary.DicomSeriesInstanceUid);
                       var imageIdentifier= Identifier.CreateImageIdentifier(
                            Identifier.CreatePatientKeyFromDicomObject(dicomObjectBeforeImport),
                            studyInstanceId, seriesInstanceId, sopInstanceid);
                        var storageKey = new StorageKey(DeviceConfigurationManager.PrimaryDeviceId, imageIdentifier);
                        storageKeyCollection.Add(storageKey);
                        ImportSourceInfo sourceInfo = new ImportSourceInfo();
                        sourceInfo.sourceName = soruceFile;
                        sourceInfo.beforeImportByteArray =
                            (dicomObjectBeforeImport.GetBulkData(PixelDataAsBytesTag) as MemoryStream)?.ToArray();
                        sourceInfo.storgeKey = storageKey;
                        var sourceMsg = string.Format("Soruce file:{0} and storageKey{1}", sourceInfo.sourceName, sourceInfo.storgeKey);
                        CustomLog(sourceMsg,@"c:\SourceFileInfo.txt");

                        var key = seriesInstanceId + sopInstanceid;
                        if (!beforeImport.ContainsKey(key))
                        {
                            beforeImport.Add(key, sourceInfo);
                        }

                    }

                }
            }
            var log =
                string.Format("Before import count: {0}*****************\n",beforeImport.Count);
            CustomLog(log);

            var dicomObjectCollection = QueryManager.Query(DeviceConfigurationManager.PrimaryDeviceId,
                QueryLevel.Series, null, DicomFilter.MatchAll());

            CustomLog("Total series queries " + dicomObjectCollection.Count);

            //var storagekeys = new StorageKeyCollection(storageKeyCollection);
            //var imageCollection = LoadManager.LoadFullHeaders(storagekeys);

            //CustomLog("Image count "+imageCollection.Count);
            foreach (var pdo in dicomObjectCollection)
            {
                var imageCollection = LoadManager.LoadFullHeaders(pdo.StorageKey);

                foreach (var image in imageCollection)
                {

                    string sopClassid = image.Header.GetString(DicomDictionary.DicomSopClassUid);

                    if (sopClassid == "1.2.840.10008.5.1.4.1.1.4")
                    {
                        string imageFile = image.Header.GetBulkDataReference(PixelDataAsBytesTag).FileName;
                        string seriesid = image.Header.GetString(DicomDictionary.DicomSeriesInstanceUid);
                        string sopInstanceid = image.Header.GetString(DicomDictionary.DicomSopInstanceUid);
                        string studyInstanceId = image.Header.GetString(DicomDictionary.DicomStudyInstanceUid);
                        string patientId = image.Header.GetString(DicomDictionary.DicomPatientId);
                        string patientName = image.Header.GetString(DicomDictionary.DicomPatientName);
                        var key = seriesid + sopInstanceid;
                        var bytesafterImport =
                            (image.Header.GetBulkData(PixelDataAsBytesTag) as MemoryStream).ToArray();
                        //Check if byte sequence is same

                        bool isSame = false;
                        if (beforeImport.ContainsKey(key))
                        {
                            var beforeImprtByteArray = beforeImport[key];
                            isSame = bytesafterImport.SequenceEqual(beforeImprtByteArray.beforeImportByteArray);

                            if (!isSame)
                            {
                                var message =
                                    string.Format(
                                        "************FAIL: Byte Array Sequence is not same for Soruce:{0} and target: {1}*****************\n",
                                        beforeImprtByteArray.sourceName, imageFile);
                                CustomLog(message);

                            }
                            else
                            {
                                var message =
                                    string.Format(
                                        "************PASS: Byte Array Sequence is same for Soruce:{0} and target: {1}*****************\n",
                                        beforeImprtByteArray.sourceName, imageFile);
                                CustomLog(message);
                            }
                        }
                        else
                        {
                            var message = string.Format(
                                "************NotFound key:{0}, patientId {1}, patientName {2}, studyID: {3} seriesID:{4}, sopId:{5}, image: {6} *****************\n",
                                key,patientId, patientName, studyInstanceId, seriesid, sopInstanceid, imageFile);
                            CustomLog(message);
                        }
                    }

                }
            }

            CustomLog("Comparision is completed");
                //File.WriteAllText(@"c:\temp\data.txt", byteArraySize);

                //DicomObject dicomObjectBeforeImport = DicomObject.CreateInstance(@"C:\TestData\Test\AS_E1CS_20001107161009_1_IM.dcm");
                //var bytesBeforeImport = dicomObjectBeforeImport.GetBulkData(PixelDataAsBytesTag);
                //Console.WriteLine("Bytes before import: " + bytesBeforeImport.Length);

                //DicomObject dicomObjectAferImport = DicomObject.CreateInstance(@"C:\Data\DataServer\Database\Bulk\S72430\S32010\00001\Image0001.dcm");
                //var bytesAfterImport = dicomObjectAferImport.GetBulkData(PixelDataAsBytesTag);
                //Console.WriteLine("Bytes after import: "+ bytesAfterImport.Length);

                Console.WriteLine("Press key to exit");
                Console.ReadLine();
            
        }
        internal static void CustomLog(string logMessage, string fileName = @"c:\MyLog.txt")
        {
            lock (objLock)
            {
                using (System.IO.StreamWriter txtWriter =
                    new System.IO.StreamWriter(fileName, true))
                {
                    txtWriter.WriteLine("{0} : {1}",
                        System.DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss.fff tt"),
                        logMessage);
                }
            }
            
        }


        //private static bool SplitMethod(string[] args)
        //{
        //    Console.WriteLine("BootstrapPath, DeviceId, StudyId,SeriesId");
        //    if (args.Length < 4)
        //    {
        //        Console.WriteLine("Please pass the required arguments");
        //        Console.WriteLine("Press key to exit");
        //        Console.ReadLine();
        //        return true;
        //    }

        //    var bootstrapPath = args[0];
        //    var deviceId = args[1];
        //    var studyId = args[2];
        //    var seriesId = args[3];
        //    DataServerBootstrap.Execute(bootstrapPath);

        //    var seriesIdentifier =
        //        Identifier.CreateSeriesIdentifier(Identifier.CreateDummyPatientKey(), studyId, seriesId);

        //    var storageKey = new StorageKey(deviceId, seriesIdentifier);


        //    var persistentDicomCollection = LoadManager.LoadFullHeaders(storageKey);
        //    if (persistentDicomCollection == null)
        //    {
        //        Console.WriteLine("No results returned from query. Aborting");
        //        Console.WriteLine("Press key to exit");
        //        Console.ReadLine();
        //        return true;
        //    }

        //    Console.WriteLine("Got " + persistentDicomCollection.Count + "images");
        //    var newStudyId = DicomObject.GenerateDicomUid();
        //    var newSeriesId = DicomObject.GenerateDicomUid();
        //    foreach (var persistentDicomObject in persistentDicomCollection)
        //    {
        //        persistentDicomObject.Header.SetString(DicomDictionary.DicomStudyInstanceUid, newStudyId);
        //        persistentDicomObject.Header.SetString(DicomDictionary.DicomSeriesInstanceUid, newSeriesId);
        //        var dicomObject = persistentDicomObject.ToDicomObject(true);
        //        StoreManager.StoreComposite(deviceId, dicomObject);
        //    }

        //    Console.WriteLine("Store completed");
        //    return false;
        //}

        //private static void OnStudyAdded(object sender, StudyAddedEventArgs studyAddedEventArgs)
        //{
        //    Console.WriteLine("studyAdded triggered");
        //}
        //private static void OnStudyAttributesModified(object sender, StudyModifiedEventArgs studyAddedEventArgs)
        //{
        //    Console.WriteLine("studyAttributesModified triggered");
        //}

        //private static void OnStudyUpdate(object sender, StudyUpdatedEventArgs studyAddedEventArgs)
        //{
        //    Console.WriteLine("Study updated triggered");
        //}
        //private static void OnStudyDeleted(object sender, StudyDeletedEventArgs studyAddedEventArgs)
        //{
        //    Console.WriteLine("Study deleted triggered");
        //}
        //private static void OnStudyCompleted(object sender, StudyCompletedEventArgs studyAddedEventArgs)
        //{
        //    Console.WriteLine("Study Completed triggered");
        //}
        //private static void OnSeriesAdded(object sender, SeriesAddedEventArgs studyAddedEventArgs)
        //{
        //    Console.WriteLine("SeriesAdded triggered");
        //}

        //private static void OnSeriesModified(object sender, SeriesModifiedEventArgs studyAddedEventArgs)
        //{
        //    Console.WriteLine("Series modified");
        //}
        //private static void OnSeriesCompleted(object sender, SeriesCompletedEventArgs studyAddedEventArgs)
        //{
        //    Console.WriteLine("series completed triggered");
        //}
        //private static void OnSeriesDeleted(object sender, SeriesDeletedEventArgs studyAddedEventArgs)
        //{
        //    Console.WriteLine("SeriesDeleted triggered");
        //}

        //private static void JobService()
        //{
        //    JobManager.StatusChanged += JobManagerOnStatusChanged;
        //    var seriesIdentifier = Identifier.CreateSeriesIdentifier(
        //        Identifier.CreateDummyPatientKey(), "1.2.840.113619.2.1.2.139348932.602501178",
        //        "1.2.840.113619.2.1.1.318790346.551.841082886.260");
        //    StorageKeyCollection collection = new StorageKeyCollection(new StorageKey("LocalDatabase", seriesIdentifier));
        //    StringCollection stringCollection = new StringCollection();
        //    stringCollection.Add("c7635da5-beb0-4eb6-a3a1-a214d9c9a0c3");
        //    var performerParameters = PerformerUtility.GetCopyPerformerParameters(collection, stringCollection);
        //    string message = "ABC";
        //    byte[] bytes = Encoding.ASCII.GetBytes(message);



        //    ApplicationContext context = new ApplicationContext();
        //    context.SubmitterPrivateInfo = bytes;
        //    var id = JobManager.Submit("Export", performerParameters[0].PerformerType,
        //        performerParameters[0].InParameters.ToArray(), "Naveen", context);




        //}

        //private static void JobManagerOnStatusChanged(object sender, JobInfoEventArgs e)
        //{
        //    // Check if the same job status changed event is received.
        //    Console.WriteLine(
        //        "{0} Status of job {1}, changed to {2}",
        //        DateTime.Now.ToLongTimeString(),
        //        e.JobId,
        //        e.Info.JobStatus);
        //    if (e.Info.JobStatus == JobStatus.Completed)
        //    {
        //        Console.WriteLine("Job completed");
        //        var context = JobManager.GetApplicationContext(e.JobId);
        //        var message = Encoding.ASCII.GetString(context.SubmitterPrivateInfo);
        //    }

        //    if (e.Info.JobStatus == JobStatus.PermanentlyFailed)
        //    {
        //        Console.WriteLine("Job Failed");
        //    }
        //}

        //private static void DeviceConfiguration()
        //{
        //    var deviceConfiguration =
        //        DeviceConfigurationManager.GetNetworkDeviceConfiguration("6b5f8c45-25b3-4384-b68a-90526fd22282");

        //    DicomConfigurationToolkit toolkit = new DicomConfigurationToolkit("localhost", false, 10 * 60000, true);
        //    var devices = toolkit.GetDevices(NetworkDeviceType.Remote);

        //    foreach (var networkDevice in devices)
        //    {
        //        networkDevice.AdditionalConfiguration["enableCombineMRRescaling"] = false;
        //        networkDevice.AdditionalConfiguration.Add("Test1", "Value1");
        //        networkDevice.AdditionalConfiguration.Add("Test2", "Value2");
        //        //networkDevice.
        //        //toolkit.UpdateDevice(networkDevice);
        //        toolkit.AddRemoteDevice(networkDevice);
        //    }

        //    var devicesAfterUpdate = toolkit.GetDevices(NetworkDeviceType.Remote);

        //    NetworkDevice device = new NetworkDevice();
        //    device.DeviceId = "Naveen1";
        //    device.PureDicom = false;
        //    device.DisplayName = "Naveen";
        //    device.NetworkDeviceType = NetworkDeviceType.Remote;


        //    Connection connection = new Connection();
        //    connection.DisplayName = "Store";
        //    connection.AETitle = "StoreSCP";
        //    connection.HostName = "Localhost";
        //    connection.PortNumber = 105;
        //    device.Connections.Add(connection);

        //}

        //private static void GetBulk()
        //{
        //    try
        //    {
        //        var files = Directory.GetFiles(@"C:\WorkingFolder\1.3.46.670589.11.89.5.0.6904.20190827191953061");
        //        foreach (var file in files)
        //        {
        //            MemoryStream stream = new MemoryStream();
        //            var fs = new FileStream(file, FileMode.Open, FileAccess.Read);
        //            fs.CopyTo(stream);
        //            stream.Seek(0, SeekOrigin.Begin);
        //            var dicomObject = DicomObject.CreateInstance();
        //            dicomObject.SetBulkData(DicomDictionary.DicomRedPaletteColorLookupTableData, stream);
        //            var bulkdData = dicomObject.GetBulkData(DicomDictionary.DicomRedPaletteColorLookupTableData);
        //            fs.Close();
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e);
        //        throw;
        //    }
        //}
        //private static void GetBulk2()
        //{
        //    try
        //    {
        //        var files = Directory.GetFiles(@"C:\WorkingFolder\1.3.46.670589.11.89.5.0.6904.20190827191953061", "irc20190827191953161--1775101956.blk");
        //        foreach (var file in files)
        //        {
        //            var dicomObject = DicomObject.CreateInstance();
        //            using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read))
        //            {
        //                BulkDataReference bulkDataReference = new BulkDataReference(file,0,(int)fs.Length);
        //                dicomObject.SetBulkDataReference(DicomDictionary.DicomRedPaletteColorLookupTableData,bulkDataReference);
        //            }

        //            var bulkdData = dicomObject.GetBulkData(DicomDictionary.DicomRedPaletteColorLookupTableData);

        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e);
        //        throw;
        //    }
        //}
        //private static void Store()
        //{
        //    DataModificationEvents.SubscribeToImageAddedEvent("LocalDatabase", OnImageAdded);
        //    //var files = Directory.GetFiles(@"D:\WorkingFolder\MRPerf\SmartToolPerf\smart_generated_data\DICOM");
        //    //var imageCollection = new List<DicomObject>();
        //    //foreach (var file in files) {
        //    //    var dicomObject = DicomObject.CreateInstance(file);
        //    //    imageCollection.Add(dicomObject);
        //    //}


        //    //var storeSession = StoreManager.CreateStoreSession("LocalDatabase");
        //    //storeSession.SessionCompleted += OnStoreCompleted;
        //    //var stopWatch = Stopwatch.StartNew();
        //    //StoreImagesIntoDbAsync(imageCollection, storeSession);
        //    //storeCompletedEvent.WaitOne();
        //    //stopWatch.Stop();
        //    //Console.WriteLine("Time taken " + stopWatch.ElapsedMilliseconds);
        //    //storeSession.SessionCompleted -= OnStoreCompleted;
        //}

        //private static void OnImageAdded(object sender, ImageAddedEventArgs e) {
        //    Console.WriteLine("Image Added");
        //    _counter++;
        //    Console.WriteLine("Image Added: Counter "+_counter);
        //    //var results = QueryManager.Query("LocalDatabase", QueryLevel.Image, e.Key.Identifier,
        //    //    DicomFilter.MatchAll());
        //    //Thread.Sleep(500);
        //    var results = LoadManager.LoadFastHeaders(e.Key);
        //    string message = "Image Added: Counter " + _counter+ " QueryResults: "+ results.Count;

        //    Console.WriteLine(message);
        //    CustomLog(message);
        //    //storeCompletedEvent.Set();
        //}
        //private static void OnStoreCompleted(object sender, SessionCompletedEventArgs e) {
        //    //storeCompletedEvent.Set();
        //}
        //private static void StoreImagesIntoDbAsync(IList<DicomObject> compositeImageCollection, StoreSession storeSession) {
        //    foreach (var compositeImage in compositeImageCollection) {
        //        storeSession.StoreComposite(compositeImage);
        //    }
        //    // Flush all the images
        //    storeSession.FinalizeSessionAsync();
        //}
        //internal static void CustomLog(string logMessage)
        //{
        //    using (System.IO.StreamWriter txtWriter =
        //        new System.IO.StreamWriter(@"C:\WorkingFolder\File.txt", true))
        //    {
        //        txtWriter.WriteLine("{0} : {1}",
        //            System.DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss.fff tt"),
        //            logMessage + " PID: " + Process.GetCurrentProcess().Id + " ThreadID: " +
        //            Thread.CurrentThread.ManagedThreadId);
        //    }
        //}

    }

}
