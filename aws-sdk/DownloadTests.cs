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
        public async Task SendEmailViaPopeye()
        {
            var filePath = "c:/temp/test.jpg";
            var token = await S3Service.GetS3Token("upload");
            await S3Service.UploadFile(token, filePath);

            var popeyeApiUrl = ConfigurationManager.AppSettings["PopeyeBaseUrl"];
            var popeyeApiDeveloperKey = ConfigurationManager.AppSettings["MyobApiKey"];

            var config = new PopeyeConfiguration(popeyeApiDeveloperKey, popeyeApiUrl);
            var _emailClient = new PopeyeClient(config);

            var request = BuildEmailRequest();
            request.Attachments = new[]
            {
                new PopeyeAttachment()
                {
                   FileName = Path.GetFileName(filePath),
                   Mime="application/jpg",
                   S3Key = string.Format("{0}/{1}", token.path, Path.GetFileName(filePath)),
                   UploadPassword = token.uploadPassword
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
                            Uid = "123",
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
                    Subject = "Test Email" + DateTime.Now,
                    WebHook = new PopeyeWebhook {Url = @"https://localhost:8080", Headers = new PopeyeWebhookHeaders()},
                },
               
            };
            return request;
        }
    }
}
