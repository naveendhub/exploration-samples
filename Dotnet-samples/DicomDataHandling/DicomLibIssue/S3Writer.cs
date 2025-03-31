using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Transfer;

namespace DicomLibIssue
{
    internal class S3Writer:IDicomWriter
    {
        private IAmazonS3 amazonS3Client;
        private TransferUtility transferUtility;
        private readonly string serviceUrl;
        private readonly string bucketName;
        private readonly string accesskeyId;
        private readonly string secretAccesskey;
        private readonly double timeOutInSeconds;
        private readonly int transferUtilityConcurrentServiceRequests;
        private readonly string folderPrefix;
        public S3Writer()
        {
            serviceUrl = "http://127.0.0.1:9000";
            bucketName = "dicomdata";
            accesskeyId = "minio";
            secretAccesskey = "minio123";
            timeOutInSeconds = 100;
            transferUtilityConcurrentServiceRequests = 10;
            folderPrefix = "_RAW";
            InitializeS3Client();   
        }
        private void InitializeS3Client()
        {
            // Create S3 Configuration Object
            var config = new AmazonS3Config
            {
                ServiceURL = serviceUrl,
                ForcePathStyle = true,
                RetryMode = RequestRetryMode.Standard,
            };

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // refer https://github.com/aws/aws-sdk-net/issues/1929
                config.HttpClientCacheSize = 1;
            }
            
            config.Timeout = TimeSpan.FromSeconds(timeOutInSeconds);
            
            amazonS3Client = new AmazonS3Client(accesskeyId, secretAccesskey, config);
            transferUtility = new TransferUtility(amazonS3Client,
                    new TransferUtilityConfig { ConcurrentServiceRequests = transferUtilityConcurrentServiceRequests });
            
        }
        public async Task Store(Stream instanceStream)
        {
            instanceStream.Seek(0, SeekOrigin.Begin);
            var fileName = Path.GetRandomFileName();
            var objectKey = $"{folderPrefix}/{fileName}";
            var uploadMultipartRequest = new TransferUtilityUploadRequest
            {
                BucketName =bucketName,
                Key = objectKey,
                InputStream = instanceStream
            };

            // write to s3
            await transferUtility.UploadAsync(uploadMultipartRequest);

        }
    }
}
