using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using Newtonsoft.Json;

namespace aws_sdk
{
    public class S3Service
    {
        public static async Task<dynamic> GetS3Token(string endpoint)
        {
            using (var httpClient = new HttpClient())
            {
                var baseurl = ConfigurationManager.AppSettings["PopeyeBaseUrl"];
                if (string.IsNullOrEmpty(baseurl)) throw new Exception("PopeyeBaseUrl value is not found in the config.");
                if (!baseurl.EndsWith("/")) baseurl += "/";

                httpClient.BaseAddress = new Uri(baseurl);
                httpClient.DefaultRequestHeaders.Add("x-mashery-message-id", ConfigurationManager.AppSettings["MasheryMessageId"]);
                httpClient.DefaultRequestHeaders.Add("x-myobapi-key", ConfigurationManager.AppSettings["MyobApiKey"]);

                var response = await httpClient.GetStringAsync(endpoint);
                Trace.WriteLine(response);
                dynamic result = JsonConvert.DeserializeObject(response);

                return result;
            }
        }

        public static async Task UploadFile()
        {
            var token = await S3Service.GetS3Token("upload");
            UploadFile(token, "c:/temp/test.jpg");
        }

        public static async Task UploadFile(dynamic token, string filePath)
        {
            try
            {
                string keyName = string.Format("{0}/{1}", token.path, Path.GetFileName(filePath));

                var client = new AmazonS3Client(token.accessKeyId, token.secretAccessKey, token.sessionToken, RegionEndpoint.APSoutheast2);
                var fileTransferUtility = new TransferUtility(client);

                var request = new TransferUtilityUploadRequest
                {
                    BucketName = token.bucket,
                    FilePath = filePath,
                    Key = keyName,
                    PartSize = 6291456, // 6 MB.
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
                };

                await fileTransferUtility.UploadAsync(request);
            }
            catch (AmazonS3Exception s3Exception)
            {
                Console.WriteLine(s3Exception.Message, s3Exception.InnerException);
            }
        }
    }
}
