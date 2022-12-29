using NUnit.Framework;
using ServiceStack.Aws.FileStorage;

namespace ServiceStack.Aws.Tests.FileStorage
{
    [TestFixture]
    public class InMemoryStorageProviderTests : FileStorageProviderCommonTests
    {
        [OneTimeSetUp]
        public void FixtureSetup()
        {
            providerFactory = () => InMemoryFileStorageProvider.Instance;
            baseFolderName = TestSubDirectory;
            Initialize();
        }
    }
}
