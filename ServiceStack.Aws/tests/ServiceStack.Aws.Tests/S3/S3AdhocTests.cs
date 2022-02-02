using Amazon.S3;
using Amazon.S3.Model;
using NUnit.Framework;
using ServiceStack.Aws;
using ServiceStack.Text;

namespace ServiceStack.Aws.Tests.S3
{
    [Explicit("Ignore IntegrationTest")]
    [TestFixture]
    public class S3AdhocTests
    {
        IAmazonS3 client;
        private const string BucketName = "ss-ci-test";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            client = AwsConfig.CreateAmazonS3Client();
        }

        [Test]
        public void List_Buckets()
        {
            var response = client.ListBuckets(new ListBucketsRequest());

            var bucketNames = response.Buckets.Map(x => x.BucketName);
            bucketNames.PrintDump();
        }

        public void AddTextFile(string fileName, string body)
        {
            client.PutObject(new PutObjectRequest
            {
                BucketName = BucketName,
                Key = fileName,
                ContentType = MimeTypes.PlainText,
                ContentBody = body,
                StorageClass = S3StorageClass.Standard
            });
        }

        [Test]
        public void Create_test_files_and_folders()
        {
            AddTextFile("testfile.txt", "testfile");
            AddTextFile("a/testfile-a1.txt", "testfile-a1");
            AddTextFile("a/testfile-a2.txt", "testfile-a2");
            AddTextFile("a/b/testfile-ab1.txt", "testfile-ab1");
            AddTextFile("a/b/testfile-ab2.txt", "testfile-ab2");
            AddTextFile("a/b/c/testfile-abc1.txt", "testfile-abc1");
            AddTextFile("a/b/c/testfile-abc2.txt", "testfile-abc2");
        }

        [Test]
        public void List_files()
        {
            var response = client.ListObjects(new ListObjectsRequest
            {
                BucketName = BucketName,                
            });

            response.S3Objects.Each(x =>
                x.Key.Print());
        }

        [Test]
        public void Get_File()
        {
            AddTextFile("testfile.txt", "testfile");

            var response = client.GetObject(new GetObjectRequest
            {
                BucketName = BucketName,
                Key = "testfile.txt",                
            });

            response.Key.Print();
            response.ResponseStream.ReadFully().FromUtf8Bytes().Print();

            AddTextFile("testfile.txt", "testfile");

            response = client.GetObject(new GetObjectRequest
            {
                BucketName = BucketName,
                Key = "testfile.txt",
                ModifiedSinceDateUtc = response.LastModified,
            });

            response.Key.Print();
        }
    }
}