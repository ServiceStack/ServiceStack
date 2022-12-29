using Amazon;
using NUnit.Framework;
using ServiceStack.Aws.FileStorage;

namespace ServiceStack.Aws.Tests.FileStorage
{
    [TestFixture, Category("Integration")]
    [Explicit]
    public class S3FileStorageProviderTests : FileStorageProviderCommonTests
    {

        [OneTimeSetUp]
        public void FixtureSetup()
        {
            //// In order to test with a real S3 instance, enter the appropriate auth information below and run these tests explicitly
            var s3ConnectionFactory = new S3ConnectionFactory(
                AwsConfig.AwsAccessKey,
                AwsConfig.AwsSecretKey, 
                RegionEndpoint.USEast2);

            providerFactory = () => new S3FileStorageProvider(s3ConnectionFactory);
            
            baseFolderName = TestSubDirectory;

            Initialize();
        }
    }
}
