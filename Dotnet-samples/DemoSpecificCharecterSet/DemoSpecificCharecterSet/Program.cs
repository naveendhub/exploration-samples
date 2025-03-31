using System.Text;
using Philips.Platform.Common;
using Philips.Platform.Dicom;

// Initialize dicom library.
DicomBootstrap.Execute();
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);



var dicomObject = DicomObject.CreateInstance(@"C:\Users\310167623\Downloads\PT_01.DCM");

var fileStream = new FileStream(@"C:\Workspace\Logs\GenData\test.dcm", FileMode.CreateNew);
dicomObject.Serialize(fileStream, new SerializerConfiguration {
    AddMetadata = true
});

fileStream.Close();


//var metaData = DicomObject.CreateInstance();
//metaData.SetString(
//    DicomDictionary.DicomMediaStorageSopClassUid, "1.2.840.10008.5.1.4.1.1.4");
//metaData.SetString(
//    DicomDictionary.DicomMediaStorageSopInstanceUid, "1.3.46.670589.88.42315042444161721418.23597231122846524991");
//metaData.SetString(
//    DicomDictionary.DicomTransferSyntaxUid, "1.2.840.10008.1.2.1");

//var headerStream = new MemoryStream();

//var fileStream = new FileStream(@"C:\WorkingDirectory\test.dcm", FileMode.CreateNew);
//metaData.Serialize(fileStream, new SerializerConfiguration
//{
//    //Metadata = metaData
//    //AddMetadata = true
//});

//fileStream.Close();


Console.WriteLine("Done");

//CreateObject(1, new[] { "ISO_IR 13" }, "c:\\instance13", "ﾏﾂﾀﾞｲﾗ, ﾉﾌﾞﾔｽ");
//CreateObject(2, new[] { "ISO_IR 87" }, "c:\\instance87", "ﾏﾂﾀﾞｲﾗ, ﾉﾌﾞﾔｽ");
//CreateObject(3, new[] { "ISO 2022 IR 13", "ISO 2022 IR 100" }, "c:\\instance13-100", "ﾏﾂﾀﾞｲﾗ, ﾉﾌﾞﾔｽ");
//CreateObject(4, new[] { "ISO 2022 IR 87", "ISO 2022 IR 100" }, "c:\\instance87-100", "ﾏﾂﾀﾞｲﾗ, ﾉﾌﾞﾔｽ");
//CreateObject(5, new[] { "ISO 2022 IR 100", "ISO 2022 IR 87" }, "c:\\instance100-87", "==ﾏﾂﾀﾞｲﾗ, ﾉﾌﾞﾔｽ");
//CreateObject(6, new[] { "ISO 2022 IR 100", "ISO 2022 IR 13" }, "c:\\instance100-13", "==ﾏﾂﾀﾞｲﾗ, ﾉﾌﾞﾔｽ");



//CreateObject(3, new[] { "ISO 2022 IR 166", "ISO 2022 IR 13" }, "c:\\instance166-13-console", "==ﾏﾂﾀﾞｲﾗ, ﾉﾌﾞﾔｽะพำกกดเฟ");

//CreateObject(6, new[] { "ISO 2022 IR 100", "ISO 2022 IR 13" }, "c:\\instance100-13", "==ﾏﾂﾀﾞｲﾗ, ﾉﾌﾞﾔｽ");

//CreateObject(3, new[] { "ISO 2022 IR 101", "ISO 2022 IR 58" },
//    "c:\\instance101-58-console", "=行健=Curie^Marie^Skłodowska^^N.Lr.");

//var fileName = Path.Combine(@"C:\WorkingDirectory\DefectLogs", Path.GetRandomFileName());

//var instance = DicomObject.CreateInstance(@"C:\Users\310167623\Downloads\KU5OAMZ5");
//using Stream stream = File.Open(fileName, FileMode.Create);
//instance.Serialize(stream, new SerializerConfiguration() { AddMetadata = true });

//static void CreateObject(int id, string[] charSets, string fileName, string content)
//{
//    var instance = DicomObject.CreateInstance();
//    instance.SetStringArray(DicomDictionary.DicomSpecificCharacterSet, charSets);
//    instance.SetString(DicomDictionary.DicomSopClassUid, WellKnownSopClassUids.SecondaryCaptureImageStorage);
//    instance.SetString(DicomDictionary.DicomPatientName, content);
//    using Stream stream = File.Open(fileName, FileMode.Create);
//    instance.Serialize(stream, new SerializerConfiguration() { AddMetadata = true });
//}