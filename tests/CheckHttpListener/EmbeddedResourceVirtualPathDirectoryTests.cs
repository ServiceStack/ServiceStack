using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.IO;
using ServiceStack.Testing;
using ServiceStack.VirtualPath;

namespace CheckHttpListener
{
    [TestFixture]
    public class EmbeddedResourceVirtualPathDirectoryTests
    {
        [Test]
        public void WithAnEmbeddedResource_ShouldBeAbleToFindItInVirtualDirectoryWalk()
        {
            var appHost = new BasicAppHost(GetType().Assembly);
            var directory = new ResourceVirtualDirectory(new InMemoryVirtualPathProvider(appHost), null, GetType().Assembly, GetType().Assembly.GetName().Name);

            var resourceFiles = WalkForFiles(directory).ToList();
            Assert.IsNotEmpty(resourceFiles);
            Assert.Contains("/ResourceFile.txt", resourceFiles);
        }

        private IEnumerable<string> WalkForFiles(IVirtualDirectory resourceVirtualDirectory)
        {
            return resourceVirtualDirectory.Directories.SelectMany(WalkForFiles)
                .Concat(resourceVirtualDirectory.Files.Select(f => f.VirtualPath));
        }
    }
}