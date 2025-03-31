using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Philips.Platform.Common;
using Philips.Platform.Dicom;

namespace DicomLibIssue
{
    internal class DicomParser
    {
        private SerializerConfiguration serializerConfig;
        private const int ThreasholdSizeHybridStream = 5 * 1024 * 1024;

        internal DicomParser()
        {
            serializerConfig = new SerializerConfiguration
            {
                AddMetadata = true,
                AddSpecificCharacterSetTag = true
            };
        }
        
        internal Stream CreateSinglePartStream(string imageFileName)
        {
            var dicomObject = DicomObject.CreateInstance(imageFileName);
            //var hybridStream = new HybridStream(ThreasholdSizeHybridStream);

            var hybridStream = new MemoryStream();
            var transferSyntax = dicomObject.GetString(DicomDictionary.DicomTransferSyntaxUid);
            serializerConfig.TransferSyntax = transferSyntax;

            dicomObject.Serialize(hybridStream, serializerConfig);
            hybridStream.Seek(0, SeekOrigin.Begin);
            return hybridStream;
        }
    }
}
