using System;
using System.Collections.Generic;
using System.IO;

using Philips.Platform.Common;
using Philips.Platform.Dicom;

namespace DicomLibraryExploration {
    internal class Program {
        static void Main(string[] args) {
            var file = @"C:\Workspace\Logs\CT-MaskingData\Surview__ScanMdu_20_03_2025_16_05 1.mdu";
            var dependencyProvider = new CustomDepedencyProvider();
            dependencyProvider.RegisterDependencyProvider();

            DicomBootstrap.Execute();


            ReadOldMDUData(file);



            Console.ReadLine();
        }

        private static void ReadOldMDUData(string file) {
            //DicomObject dicomObject;
            //using (var stream = new FileStream(file, FileMode.Open)) {
            //    dicomObject = DicomObject.CreateInstance(stream);
            //}
            var dicomObject = DicomObject.CreateInstance();
            byte[] expectedArray = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09 };
            var memoryStream = new MemoryStream(expectedArray);

            dicomObject.SetBulkData(PhilipsDictionary.ElscintOfsetListStructure, memoryStream);

            var bulkData = dicomObject.GetBulkData(PhilipsDictionary.ElscintOfsetListStructure);

            //bool result = LetsMask(dicomObject);
            //var filePath = CreateScanFile();
            var filePath = @"C:\Workspace\Logs\CT-logs\out.dcm";
            using (var stream = new FileStream(filePath, FileMode.Create)) {
                dicomObject.Serialize(stream);
            }
            
            Console.ReadLine();
        }

        private static bool LetsMask(DicomObject dicomObject) {
            var preferredImplementorTags = new Dictionary<string, IList<uint>>();

            List<uint> tagsList = new List<uint>
            {
                0x00e10010, 0x01e10010, 0x01f10010, 0x01f70010, 0x01f90010, 0x20090010, 0x00030010
            };

            preferredImplementorTags.Add("ELSCINT1", tagsList);

            //In-Memory dicom object
            var allocator = new PreferredImplementorTagsSlotAllocator(preferredImplementorTags);
            bool isSuccess = allocator.TryAllocate(dicomObject);
            return isSuccess;
            //Serialize to target
        }

        private static string CreateScanFile() {
            var filePath = @"C:\Workspace\Logs\CT-logs";
            // string date = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss", LocaleProvider.InvariantCulture);
            string date = DateTime.Now.ToString("dd_MM_yyyy_HH_mm");
            date = date.Replace(':', '_');
            date = date.Replace('/', '_');
            var fileName = "Surview" + "_" + "_ScanMdu_" + date;
            var extension = "mdu";
            var totalFilename = string.Format("{0}.{1}", fileName, extension);
            var totalPath = Path.Combine(filePath, totalFilename);
            return totalPath;
        }
    }
}
