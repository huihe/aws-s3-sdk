using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using MYOB.Popeye.SDK;
using MYOB.Popeye.SDK.Contracts;
using MYOB.Popeye.SDK.Services;
using NUnit.Framework;

namespace aws_sdk
{
    [TestFixture]
    public class DownloadTests
    {
       
        [Test]
        public async Task DownloadTestFile()
        {
            var token = await S3Service.GetS3Token("upload");

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

        [Test]
        public void SendEmailViaPopeye()
        {
            var popeyeApiUrl = ConfigurationManager.AppSettings["PopeyeApiUrl"];
            var popeyeApiDeveloperKey = ConfigurationManager.AppSettings["PopeyeApiDeveloperKey"];

            var config = new PopeyeConfiguration(popeyeApiDeveloperKey, popeyeApiUrl);
            var _emailClient = new PopeyeClient(config);

            var request = BuildEmailRequest();
            request.Attachments = new[]
            {
                new PopeyeAttachment()
                {
                   FileName = "test.jpg",
                   Mime="application/jpg",
                   S3Key = "2015/10/26/d141ac5784546e632c0cb0539ab6dfae/test.jpg",
                   UploadPassword = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJwYXRoIjoiMjAxNS8xMC8yNi9kMTQxYWM1Nzg0NTQ2ZTYzMmMwY2IwNTM5YWI2ZGZhZSIsImV4cGlyZXMiOiIyMDE1LTEwLTI2VDA2OjI0OjQ1LjAwMFoifQ.dh6yD00Xloer--lHfaOMixwM4qaEaZNW88elRtOmB5o",
                }
            };

            var response = _emailClient.SendEmailAsync(request).Result;
            Assert.AreEqual(HttpStatusCode.Accepted, response.HttpStatus);
        }

        private EmailRequest<MessageMetaData, PopeyeTemplateVariables> BuildEmailRequest()
        {
            var request = new EmailRequest<MessageMetaData, PopeyeTemplateVariables>
            {
                Body = new EmailRequestBody<MessageMetaData, PopeyeTemplateVariables>
                {
                    From = new PopeyeEmail
                    {
                        Email = "test@myob.com",
                        Name = "dev test"
                    },
                    To = new[]
                    {
                        new PopeyeEmail
                        {
                            Email = "hui.he@myob.com",
                            Name = "email to"
                        }
                    },
                    MetaData = new MessageMetaData
                    {
                        Company = new Company
                        {
                            Uid = CompanyFileId,
                            Name = "dev test company id"
                        },
                        Document = new Document { DocumentNumber = "PaySlip 001", DocumentType = ResourceType.PaySlip }
                    },
                    Template = new PopeyeTemplate<PopeyeTemplateVariables>
                    {
                        Name = "generic",
                        Variables = new PopeyeTemplateVariables
                        {
                            CompanyName = "MYOB",
                            Text = "this is the email content",
                            Html = string.Empty,
                            HeadingColour = string.Empty,
                        }
                    },
                    Subject = "Test Email",
                    WebHook = new PopeyeWebhook {Url = @"https://localhost:8080", Headers = new PopeyeWebhookHeaders()},
                },
               
            };
            return request;
        }
    }
}
