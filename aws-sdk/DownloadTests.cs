using System;
using System.IO;
using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using NUnit.Framework;

namespace aws_sdk
{
    [TestFixture]
    public class DownloadTests
    {
       
        [Test]
        public void DownloadTestFile()
        {
            var token = S3Service.GetS3Token("upload");

                string filePath = "c:/temp/download/test.jpg";
                string awsAccessKeyId = token.accessKeyId;
                string awsSecretAccessKey = token.secretAccessKey;
                string awsSessionToken = token.sessionToken;
                string existingBucketName = token.bucket;
                string keyName = string.Format("{0}/{1}", token.path, Path.GetFileName(filePath));

                var client = new AmazonS3Client(awsAccessKeyId, awsSecretAccessKey, awsSessionToken, RegionEndpoint.APSoutheast2);
                var fileTransferUtility = new TransferUtility(client);

                var request = new TransferUtilityDownloadRequest()
                {
                    BucketName = existingBucketName,
                    FilePath = filePath,
                    Key = keyName,
                    ServerSideEncryptionCustomerMethod = ServerSideEncryptionCustomerMethod.AES256,
                    ServerSideEncryptionCustomerProvidedKey = token.uploadPassword,
                };

                fileTransferUtility.Download(request);
                Console.WriteLine("download 1 completed");
        }
    }
}
