using System;

namespace aws_sdk
{
    class Program
    {
        static void Main(string[] args)
        {
            S3Service.UploadFile().Wait();
            Console.ReadKey();
        }
       
    }
}
