using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using Newtonsoft.Json;

namespace aws_sdk
{
    public class S3Service
    {
        public static dynamic GetS3Token(string endpoint)
        {
            using (var httpClient = new HttpClient())
            {
                var baseurl = ConfigurationManager.AppSettings["PopeyeBaseUrl"];
                if (string.IsNullOrEmpty(baseurl)) throw new Exception("PopeyeBaseUrl value is not found in the config.");
                if (!baseurl.EndsWith("/")) baseurl += "/";

                httpClient.BaseAddress = new Uri(baseurl);
                httpClient.DefaultRequestHeaders.Add("x-mashery-message-id", ConfigurationManager.AppSettings["MasheryMessageId"]);
                httpClient.DefaultRequestHeaders.Add("x-myobapi-key", ConfigurationManager.AppSettings["MyobApiKey"]);

                var response = httpClient.GetStringAsync(endpoint).Result;
                dynamic result = JsonConvert.DeserializeObject(response);

                return result;
            }
        }

        public static void UploadFile()
        {
            try
            {
                var token = S3Service.GetS3Token("upload");

                string filePath = "c:/temp/test.jpg";
                string awsAccessKeyId = token.accessKeyId;
                string awsSecretAccessKey = token.secretAccessKey;
                string awsSessionToken = token.sessionToken;
                string existingBucketName = token.bucket;
                string keyName = string.Format("{0}/{1}", token.path, Path.GetFileName(filePath));
                string password = token.uploadPassword;

                var client = new AmazonS3Client(awsAccessKeyId, awsSecretAccessKey, awsSessionToken, RegionEndpoint.APSoutheast2);
                var fileTransferUtility = new TransferUtility(client);

                var request = new TransferUtilityUploadRequest
                {
                    BucketName = existingBucketName,
                    FilePath = filePath,
                    StorageClass = S3StorageClass.ReducedRedundancy,
                    PartSize = 6291456, // 6 MB.
                    Key = keyName,
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
                };

                fileTransferUtility.Upload(request);
                Console.WriteLine("Upload 1 completed with upload password " + password);

                //// 2. Specify object key name explicitly.
                //fileTransferUtility.Upload(filePath, existingBucketName, keyName);
                //Console.WriteLine("Upload 2 completed");

                //// 3. Upload data from a type of System.IO.Stream.
                //using (FileStream fileToUpload = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                //{
                //    fileTransferUtility.Upload(fileToUpload, existingBucketName, keyName);
                //}
                //Console.WriteLine("Upload 3 completed");

                //// 4.Specify advanced settings/options.
                //TransferUtilityUploadRequest fileTransferUtilityRequest = new TransferUtilityUploadRequest
                //{
                //    BucketName = existingBucketName,
                //    FilePath = filePath,
                //    StorageClass = S3StorageClass.ReducedRedundancy,
                //    PartSize = 6291456, // 6 MB.
                //    Key = keyName,
                //    CannedACL = S3CannedACL.PublicRead
                //};
                //fileTransferUtilityRequest.Metadata.Add("param1", "Value1");
                //fileTransferUtilityRequest.Metadata.Add("param2", "Value2");
                //fileTransferUtility.Upload(fileTransferUtilityRequest);
                //Console.WriteLine("Upload 4 completed");

               
            }
            catch (AmazonS3Exception s3Exception)
            {
                Console.WriteLine(s3Exception.Message, s3Exception.InnerException);
            }
        }
    }
}
