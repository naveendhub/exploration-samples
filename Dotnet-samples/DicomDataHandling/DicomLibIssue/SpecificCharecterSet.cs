using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Philips.Platform.Common;
using Philips.Platform.Dicom;

namespace DicomLibIssue
{
    internal static class SpecificCharecterSet
    {
        internal static void ValidateSpecificCharecterSetBehavior()
        {
            var dcmInstance = DicomObject.CreateInstance(@"C:\TestData\OrigIssueImage\KU056Y91");
            //dcmInstance.Remove(DicomDictionary.DicomSpecificCharacterSet);
            //dcmInstance.SetStringArray(DicomDictionary.DicomSpecificCharacterSet, new[] { "ISO_IR 192" });

            dcmInstance.SetStringArray(DicomDictionary.DicomSpecificCharacterSet,
                new[] { "ISO 2022 IR 100", "ISO 2022 IR 13" });

            dcmInstance.SetString(DicomDictionary.DicomPatientName, "==マツダイラ, ノブヤス");

            //var identifier = new SpecificCharacterSetIdentifier(new SpecificCharacterSetIdentifierForString());
            //identifier.DeduceSpecificCharacterSetValue((Philips.Platform.Dicom.Data.DicomObject)dcmInstance);

            var fileName = Path.Combine(@"C:\WorkingDirectory\DefectLogs", Path.GetRandomFileName());

            using (Stream stream = File.Open(fileName, FileMode.Create))
            {
                dcmInstance.Serialize(stream,
                    new SerializerConfiguration()
                    {
                        AddMetadata = true,
                        TransferSyntax = WellKnownTransferSyntaxes.ExplicitVrLittleEndian.Uid,
                    });
            }
        }
    }
}
