using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Philips.Platform.ApplicationIntegration;
using Philips.Platform.ApplicationIntegration.DataAccess;
using Philips.Platform.Common;
using Philips.Platform.Common.DataAccess;
using Philips.Platform.Dicom;
using Philips.Platform.ScheduledWorkFlowToolkit;
using Philips.Platform.SystemIntegration;
using Philips.Platform.SystemIntegration.Bootstrap;
using Philips.Platform.SystemIntegration.SeriesConstruction;
//using Philips.PmsMR.Platform.DataDictionaryExtension;
using DicomDictionary = Philips.Platform.ApplicationIntegration.DataAccess.DicomDictionary;

namespace DataserverClient {
    class Program {
        
        private static readonly Random random = new Random();
        private static string deviceId = "LocalDatabase";
        private static int iteration = 10000;
        private static int pid;
        static void Main(string[] args) {
           TestClient(args);

            ExitApplication();
        }

        
        private static string logFile;
        private static readonly object customLogLock = new object();

        internal static void CustomLog(string logMessage) {
            lock (customLogLock) {
                using (System.IO.StreamWriter txtWriter =
                    new System.IO.StreamWriter(logFile, true)) {
                    txtWriter.WriteLine("{0} : PID {1} : ThreadID {2} : {3}",
                        System.DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss.fff tt"),
                        pid,
                        Thread.CurrentThread.ManagedThreadId,
                        logMessage);
                }
            }
        }

        private static void TestClient(string[] args) {
            var consoleOriginalColor = Console.ForegroundColor;
            pid = GetPID();
            logFile = @"G:\MyLog_" + pid + ".txt";
            if (args == null) {
                string message =
                    "Usage: DataServerClient.exe -createStudy \n " +
                    "DataServerClient.exe -updateStudy <studyId> [iterationCount] \n" +
                    "DataServerClient.exe -updateLeaf <studyId> [iterationCount] \n " +
                    "DataServerClient.exe -load <studyId> <seriesId> [iterationCount] \n" +
                    "DataServerClient.exe -mpps <studyId> [iterationCount] \n " +
                    "DataServerClient.exe -getstudies \n" +
                    "DataServerClient.exe -updateStudyMppsLoad <studyInstanceUid> <seriesUid> [iterationCount] \n" +
                    "DataServerClient.exe -query <studyInstanceUid> <seriesUid> [iterationCount]";
                Trace(" Please pass the arguments.\n " + message);
            }

            iteration = 10000;
            var createStudy = false;
            var updateStudy = false;
            var updateLeaf = false;
            var load = false;
            var mpps = false;
            var getStudies = false;
            var studyupdateAndMpps = false;
            var query = false;
            var studyInstanceUid = string.Empty;
            var seriesInstanceUid = string.Empty;

            if (args != null && args[0].ToUpperInvariant() == "-CREATESTUDY") {
                createStudy = true;
            }

            if (args != null && args[0].ToUpperInvariant() == "-UPDATESTUDY") {
                if (args[1] == null) {
                    Trace("Pass study instance UID and iteration count");
                    ExitApplication();
                }

                studyInstanceUid = args[1];
                if (!string.IsNullOrWhiteSpace(args[2])) {
                    iteration = int.Parse(args[2]);
                }

                updateStudy = true;
            }

            if (args != null && args[0].ToUpperInvariant() == "-UPDATELEAF") {
                if (args[1] == null) {
                    Trace("Pass study instance UID");
                    ExitApplication();
                }

                studyInstanceUid = args[1];
                if (!string.IsNullOrWhiteSpace(args[2])) {
                    iteration = int.Parse(args[2]);
                }

                updateLeaf = true;
            }
            if (args != null && args[0].ToUpperInvariant() == "-LOAD") {
                if (args[1] == null || args[2] == null) {
                    Trace("Pass study instance UID, series instance UID and iteration count ");
                    ExitApplication();
                }

                studyInstanceUid = args[1];
                if (!string.IsNullOrWhiteSpace(args[2])) {
                    seriesInstanceUid = args[2];
                }
                if (!string.IsNullOrWhiteSpace(args[3])) {
                    iteration = int.Parse(args[3]);
                }
                load = true;
            }
            if (args != null && args[0].ToUpperInvariant() == "-MPPS") {
                if (args[1] == null) {
                    Trace("Pass study instance UID");
                    ExitApplication();
                }

                studyInstanceUid = args[1];
                if (!string.IsNullOrWhiteSpace(args[2])) {
                    iteration = int.Parse(args[2]);
                }

                mpps = true;
            }
            if (args != null && args[0].ToUpperInvariant() == "-GETSTUDIES") {
                getStudies = true;
            }
            if (args != null && args[0].ToUpperInvariant() == "-UPDATESTUDYMPPSLOAD") {
                if (args[1] == null || args[2] ==null) {
                    Trace("Pass study instance UID and series instance uid");
                    ExitApplication();
                }
                studyInstanceUid = args[1];
                
                if (!string.IsNullOrWhiteSpace(args[2])) {
                    seriesInstanceUid = args[2];
                }
                if (!string.IsNullOrWhiteSpace(args[3])) {
                    iteration = int.Parse(args[3]);
                }

                studyupdateAndMpps = true;
            }

            if (args != null && args[0].ToUpperInvariant() == "-QUERY") {
                if (args[1] == null || args[2] == null) {
                    Trace("Pass study instance UID, series instance UID and iteration count");
                    ExitApplication();
                }

                studyInstanceUid = args[1];
                if (!string.IsNullOrWhiteSpace(args[2])) {
                    seriesInstanceUid = args[2];
                }
                if (!string.IsNullOrWhiteSpace(args[3])) {
                    iteration = int.Parse(args[3]);
                }
                query = true;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Press key to continue");
            Console.ForegroundColor = consoleOriginalColor;
            Console.ReadLine();

            BootStrap();

            if (createStudy) {
                CreateStudy();
                ExitApplication();
            }

            if (updateStudy) {
                UpdateStudy(studyInstanceUid);
                ExitApplication();
            }

            if (updateLeaf) {
                UpdateLeaf(studyInstanceUid);
                ExitApplication();
            }

            if (load) {
                Load(studyInstanceUid, seriesInstanceUid);
                ExitApplication();
            }

            if (query) {
                Query(studyInstanceUid, seriesInstanceUid);
                ExitApplication();
            }

            if (mpps) {
                SendMPPSComplete(studyInstanceUid);
                ExitApplication();
            }

            if (getStudies) {
                GetAllStudies();
                ExitApplication();
            }

            if (studyupdateAndMpps) {
                UpdateStudyAndSendMPPS(studyInstanceUid, seriesInstanceUid);
                ExitApplication();
            }
            ExitApplication();
        }

        private static void Query(string studyInstanceUid, string seriesInstanceUid) {
            var seriesIdentifier = Identifier.CreateSeriesIdentifier(Identifier.CreateDummyPatientKey(), studyInstanceUid,
                seriesInstanceUid);
            var storageKey = new StorageKey(deviceId, seriesIdentifier);
            for (int i = 0; i < iteration; i++) {
                var stopWatch = Stopwatch.StartNew();
                var results = QueryManager.QueryChildren(storageKey);
                stopWatch.Stop();
                Trace($"Query : Result count: {results.Count} : Elapsed time(ms): {stopWatch.ElapsedMilliseconds}  IterationCount " + i);
            }
        }

        private static void UpdateStudyAndSendMPPS(string studyInstanceUid, string seriesInstanceUid) {
            Task.Factory.StartNew(() => { UpdateStudy(studyInstanceUid); });
            Task.Factory.StartNew(() => { SendMPPSComplete(studyInstanceUid);});
            Task.Factory.StartNew(() => { Load(studyInstanceUid, seriesInstanceUid); });
            Task.Factory.StartNew(() => { Query(studyInstanceUid, seriesInstanceUid); });
        }

        private static void GetAllStudies() {
            var studies = QueryManager.QueryStudy(deviceId, DicomFilter.MatchAll());
            foreach (var study in studies) {
                Trace("Study key " + study.StorageKey);
                var series = QueryManager.Query(deviceId, QueryLevel.Series, study.StorageKey.Identifier,
                    DicomFilter.MatchAll());
                foreach (var s in series) {
                    Trace("Series Key " + s.StorageKey);
                }
            }

        }

        private static void SendMPPSComplete(string studyInstanceUid) {
            for (int i = 0; i < iteration; i++) {
                ScheduledWorkflowManager.SendMppsComplete(studyInstanceUid);
                Trace("MPPS count "+i);
            }
        }

        private static void Load(string studyInstanceUid, string seriesInstanceUid) {
            var seriesIdentifier = Identifier.CreateSeriesIdentifier(Identifier.CreateDummyPatientKey(), studyInstanceUid,
                seriesInstanceUid);
            var storageKey = new StorageKey(deviceId, seriesIdentifier);
            for (int i = 0; i < iteration; i++) {
                var stopWatch = Stopwatch.StartNew();
                var results = LoadManager.LoadFullHeaders(storageKey);
                stopWatch.Stop();
                Trace($"Load : Result count: {results.Count} : Elapsed time(ms): {stopWatch.ElapsedMilliseconds} IterationCount " + i);
            }
        }

        // Generates a random string with a given size.    
        private static string RandomString(int size, bool lowerCase = false) {
            var builder = new StringBuilder(size);
            char offset = lowerCase ? 'a' : 'A';
            const int lettersOffset = 26; // A...Z or a..z: length=26  
            for (var i = 0; i < size; i++) {
                var @char = (char)random.Next(offset, offset + lettersOffset);
                builder.Append(@char);
            }
            return lowerCase ? builder.ToString().ToLower() : builder.ToString();
        }

        private static void UpdateStudy(string studyInstanceId) {
            var studyIdentifer = Identifier.CreateStudyIdentifier(Identifier.CreateDummyPatientKey(), studyInstanceId);
            var studyStorageKey = new StorageKey(deviceId, studyIdentifer);
            
            for (var i = 0; i < iteration; i++) {
                
                try {
                    var modifiedObject = DicomObject.CreateInstance();
                    var description = RandomString(10);
                    modifiedObject.SetString(DicomDictionary.DicomStudyDescription, description);
                    PatientStudyManager.UpdateStudy(studyStorageKey, modifiedObject);
                    Trace("Update study "+i);
                } catch(Exception e) {
                    Trace("Failed" + e);
                }
            }
            
        }

        private static void UpdateLeaf(string studyInstanceId) {
            var studyIdentifer = Identifier.CreateStudyIdentifier(Identifier.CreateDummyPatientKey(), studyInstanceId);
            var studyStorageKey = new StorageKey(deviceId, studyIdentifer);

            var seriesDicomObject = CreateSeriesDicomObject(studyInstanceId);
            var seriesInstanceId = DicomObject.GenerateDicomUid();
            seriesDicomObject.SetString(DicomDictionary.DicomSeriesInstanceUid, seriesInstanceId);

            var seriesSession = SeriesConstructor.BeginSeriesConstructionSession(seriesDicomObject, studyStorageKey);

            Trace("Series created "+ seriesInstanceId);
            string _blobSetSopUid = null;
                for (var i = 0; i < iteration; i++) {
                    
                    try {
                        StoreBlob(ref _blobSetSopUid, seriesSession);
                        Trace("StoreBlob: count " + i);
                    } catch (Exception e) {
                        Trace("Failed" + e);
                    }
                }
            
            seriesSession.Commit();
            Trace("Series Commited " + seriesInstanceId);
        }

        private static void StoreBlob(ref string _blobSetSopUid, SeriesConstructionSession _seriesSession) {
            DicomObject blobSet;
            if (_blobSetSopUid == null) {
                blobSet = CreateBlobSet();
                _blobSetSopUid = blobSet.GetString(DicomDictionary.DicomSopInstanceUid);
                AddBlobs(blobSet);
                _seriesSession.StoreLeaf(blobSet);
                Trace("Store leaf completed "+ _blobSetSopUid);
                
            } else {
                blobSet = _seriesSession.GetLeaf(_blobSetSopUid);
                Trace("Get leaf completed " + _blobSetSopUid);
                //add  leaf
                AddBlobs(blobSet);
                _seriesSession.UpdateLeaf(_blobSetSopUid, blobSet);
                Trace("Update leaf completed " + _blobSetSopUid);
            }
        }

        private static void AddBlobs(DicomObject blobSetObject) {
            //var blob = DicomObject.CreateInstance();
            //var blobContentArray = GetByte();
            //using (var blobStream = new MemoryStream(blobContentArray)) {
            //    blob.SetBulkData(PhilipsMRExtensionDictionary.PiimMrActualBlobData,blobStream);
            //}
            //if(blob.HasTag(PhilipsMRExtensionDictionary.PiimMrBlobObjArray)) {
            //    //remove blobarray tag from blobset object
            //    blobSetObject.Remove(PhilipsMRExtensionDictionary.PiimMrBlobObjArray);
            //}
            //blobSetObject.SetDicomObject(PhilipsMRExtensionDictionary.PiimMrBlobObjArray, new[]{blob});
            //Trace("Add blobs completed"); 
        }

        private static byte[] GetByte() {
            var rand = new Random(1000);
            var blobContent = new byte[1];
            rand.NextBytes(blobContent);
            return blobContent;
        }


        private static DicomObject CreateBlobSet() {
            var blobSet = DicomObject.CreateInstance();
            // Sop Class roid of series blobset !
            var seriesBlobsetSopClassUid = "1.3.46.670589.11.0.0.12.2";
            //set class uid
            blobSet.SetString(DicomDictionary.DicomSopClassUid, seriesBlobsetSopClassUid);
            //set SopInstance uid
            blobSet.SetString(DicomDictionary.DicomSopInstanceUid, DicomObject.GenerateDicomUid());
            return blobSet;
        }

        private static void CreateStudy() {
            var patientStudyObject = CreateStudyDicomObject();
            var studyStorageKey = PatientStudyManager.StoreStudy(patientStudyObject);
            Trace("Study created with studykey " + studyStorageKey);
        }

        private static void ExitApplication() {
            Console.WriteLine("Press key to exit");
            Console.ReadLine();
            Environment.Exit(0);
        }
        private static void BootStrap() {
            var bootstrapPath = Directory.GetCurrentDirectory();
            Trace("BootstapPath : " + bootstrapPath);
            Trace("deviceID : " + deviceId);
            DataServerBootstrap.Execute(bootstrapPath);
        }

        private static DicomObject CreateStudyDicomObject() {
            var dicomObject = DicomObject.CreateInstance();
            dicomObject.SetString(DicomDictionary.DicomAccessionNumber, "1682187032");
            dicomObject.SetString(
                DicomDictionary.DicomReferringPhysiciansName, "Johnson");
            dicomObject.SetString(DicomDictionary.DicomPatientName, "John^Alice");
            dicomObject.SetString(DicomDictionary.DicomPatientId, "72917428");
            dicomObject.SetDateTime(DicomDictionary.DicomPatientBirthDate,
                new DateTime(1986, 12, 01));
            dicomObject.SetString(DicomDictionary.DicomPatientSex, "M");
            dicomObject.SetString(DicomDictionary.DicomPatientsSize, "15");
            dicomObject.SetString(DicomDictionary.DicomPatientsWeight, "65");
            dicomObject.SetString(DicomDictionary.DicomStudyInstanceUid,
                DicomObject.GenerateDicomUid());
            dicomObject.SetString(DicomDictionary.DicomRequestingPhysician, "Watson");
            dicomObject.SetString(DicomDictionary.DicomRequestedProcedureDescription,
                "MR-Head");
            dicomObject.SetString(DicomDictionary.DicomRequestedProcedureId, "RP001");
            return dicomObject;
        }
        private static DicomObject CreateSeriesDicomObject(string StudyUid) {

            var dicomObject = DicomObject.CreateInstance();
            dicomObject.SetString(DicomDictionary.DicomStudyInstanceUid, StudyUid);
            dicomObject.SetString(DicomDictionary.DicomPatientName, "John^Alice");
            dicomObject.SetString(DicomDictionary.DicomPatientId, "72917428");
            dicomObject.SetDateTime(
                DicomDictionary.DicomPatientBirthDate, new DateTime(1986, 12, 1));
            dicomObject.SetString(DicomDictionary.DicomPatientSex, "M");
            dicomObject.SetString(DicomDictionary.DicomPatientsSize, "15");
            dicomObject.SetString(DicomDictionary.DicomPatientsWeight, "65");
            dicomObject.SetString(DicomDictionary.DicomModality, "MR");
            dicomObject.SetString(DicomDictionary.DicomInstitutionName, "SomeInstName");
            dicomObject.SetString(DicomDictionary.DicomSeriesNumber, "65412");
            dicomObject.SetString(DicomDictionary.DicomRows, "180");
            dicomObject.SetString(DicomDictionary.DicomColumns, "270");
            dicomObject.SetString(DicomDictionary.DicomSeriesDescription, "seriesDescription");
            dicomObject.SetString(DicomDictionary.DicomProtocolName, "MR-Head");
            dicomObject.SetString(Philips.Platform.ApplicationIntegration.DataAccess.PhilipsDictionary.PiimSeriesType, "MR Series");
            dicomObject.SetDateTime(DicomDictionary.DicomAcquisitionDate,
                new DateTime(
                    DateTime.Now.Year,
                    DateTime.Now.Month, DateTime.Now.Day
                )
            );
            dicomObject.SetDateTime(DicomDictionary.DicomAcquisitionDatetime, DateTime.Now);
            dicomObject.SetDateTime(DicomDictionary.DicomAcquisitionTime,
                DateTime.MinValue.Add(new TimeSpan(
                    DateTime.Now.Hour, DateTime.Now.Minute, 0))
            );

            return dicomObject;
        }

        internal static DictionaryTag GetPixelDataTag(DicomVR dicomTagVr) {
            var dicomVr = dicomTagVr == DicomVR.None ? DicomVR.OB : dicomTagVr;
            var tag = new DictionaryTag(
                DicomDictionary.DicomPixelData.Tag,
                DicomVR.OB,
                DicomDictionary.DicomPixelData.ValueMultiplicity,
                DicomDictionary.DicomPixelData.Name,
                DicomDictionary.DicomPixelData.ImplementerId);
            return tag;
        }
        private static void Trace(string message) {
            Console.WriteLine(message);
            CustomLog(message);
        }

        

        private static int GetPID() {
            return Process.GetCurrentProcess().Id;
        }

    }

}
