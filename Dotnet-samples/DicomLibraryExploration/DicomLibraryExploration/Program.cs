using System;
using System.IO;

using Philips.Platform.Common;
using Philips.Platform.CommonUtilities.DependencyInjection;
using Philips.Platform.Dicom;

namespace DicomLibraryExploration {
    internal class Program
    {
        static void Main(string[] args)
        {
            var file = @"C:\Workspace\Logs\CT-MaskingData\Surview__ScanMdu_20_03_2025_16_05 1.mdu";

            //CT needs to implement the IPrivateTagValidator interface to promote 0003 group as a valid private tag
            //IPrivateTagValidator will be available in the Philips.Platform.Dicom namespace

            //Inject your own implementation of IPrivateTagValidator using dependency container as below
            DependencyRegistration.Register<IPrivateTagValidator, TagValidator>();

            //Dicom bootstrap
            DicomBootstrap.Execute();

            //Sample code to demonstrate serialization deserialization of the dicom object with tag ElscintOfsetListStructure
            //You can observe that the tag ElscintOfsetListStructure is a private tag with a valid Private creator ELSCINT1

            byte[] expectedArray = { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09 };
            string outFile = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());

            using (var memoryStream = new MemoryStream(expectedArray))
            {

                var dicomObject = DicomObject.CreateInstance();
                dicomObject.SetBulkData(PhilipsDictionary.ElscintOfsetListStructure, memoryStream);

                //Serialize

                using (var stream = new FileStream(outFile, FileMode.Create))
                {
                    dicomObject.Serialize(stream);
                }

                //Deserialize
                DicomObject deserializedDicomObject;
                using (var stream = new FileStream(outFile, FileMode.Open))
                {
                    deserializedDicomObject = DicomObject.CreateInstance(stream);
                }

            }

            Console.ReadLine();
        }

    }
}
