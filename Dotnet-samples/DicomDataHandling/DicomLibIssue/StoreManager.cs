using System.Diagnostics;
using Amazon.S3.Model;
using Newtonsoft.Json.Serialization;
using Philips.Platform.Common;
using Philips.Platform.Dicom;

namespace DicomLibIssue
{
    internal class StoreManager
    {
        private const int MemoryThreshold = 5 * 1024 * 1024;
        private readonly SerializerConfiguration serializerConfig;
        private readonly DicomParser dicomParser;
        private readonly IDicomWriter dicomWriter;
        public StoreManager(IDicomWriter writer)
        {
            dicomWriter = writer;
            dicomParser = new DicomParser();
            serializerConfig = new SerializerConfiguration
            {
                AddMetadata = true,
                AddSpecificCharacterSetTag = true
            };
        }
        internal async Task StoreInstance(string fileName)
        {
            
            DicomObject dicomObject;
            var singlePartStream = Stream.Null;
            try
            {
                singlePartStream = dicomParser.CreateSinglePartStream(fileName);

                var stopWatch = Stopwatch.StartNew();
                
                var deserialzationTimer = Stopwatch.StartNew();
                dicomObject = DicomObject.CreateInstance(singlePartStream);
                deserialzationTimer.Stop();
                
                var remoteAeTitle = Guid.NewGuid().ToString();
                //Modify the dicomobject
                dicomObject.SetString(PhilipsDictionary.PiimRemoteAeTitle, remoteAeTitle);
                
                var hybridStream = new HybridStream(MemoryThreshold);
                var transferSyntax = dicomObject.GetString(DicomDictionary.DicomTransferSyntaxUid);
                serializerConfig.TransferSyntax = transferSyntax;
                
                var serializationTimer = Stopwatch.StartNew();
                //Serialize to hybrid stream
                dicomObject.Serialize(hybridStream, serializerConfig);
                
                serializationTimer.Stop();

                stopWatch.Stop();
                Console.WriteLine($"Time taken to deserialize and serialize  the image is" +
                                  $" {stopWatch.ElapsedTicks / 10000.0} ms and "+
                    $"Deserialization: {deserialzationTimer.ElapsedTicks /10000.0} Serializataion: {serializationTimer.ElapsedTicks/10000.0} ms" );

                hybridStream.Seek(0, SeekOrigin.Begin);
                //store to target location
                await dicomWriter.Store(hybridStream);

                await hybridStream.DisposeAsync();

                
            }
            finally
            {
                await singlePartStream.DisposeAsync();
            }
        }

    }
}
