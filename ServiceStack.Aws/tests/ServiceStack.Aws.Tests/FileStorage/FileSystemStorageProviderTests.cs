using System.IO;
using NUnit.Framework;
using ServiceStack.Aws.FileStorage;

namespace ServiceStack.Aws.Tests.FileStorage
{
    [TestFixture]
    public class FileSystemStorageProviderTests : FileStorageProviderCommonTests
    {
        [OneTimeSetUp]
        public void FixtureSetup()
        {
            providerFactory = () => FileSystemStorageProvider.Instance;
            baseFolderName = Path.Combine(Path.GetTempPath(), TestSubDirectory);
            Initialize();
        }
    }
}
