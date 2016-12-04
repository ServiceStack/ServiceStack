using NUnit.Framework;
using ServiceStack.Testing;

namespace ServiceStack.Common.Tests
{
    [TestFixture]
    public class AllowFilesTests
    {
        [Test]
        public void Does_allow_valid_FilePaths()
        {
            using (new BasicAppHost
            {
                ConfigFilter = config =>
                {
                    config.AllowFileExtensions.Add("aaa");
                    config.AllowFilePaths.Add("dir/**/*.zzz");
                }
            }.Init())
            {
                Assert.That(HttpHandlerFactory.ShouldAllow("a.js"));
                Assert.That(HttpHandlerFactory.ShouldAllow("a.aaa"));
                Assert.That(HttpHandlerFactory.ShouldAllow("dir/a/b/c/a.aaa"));
                Assert.That(!HttpHandlerFactory.ShouldAllow("a.zzz"));
                Assert.That(HttpHandlerFactory.ShouldAllow("dir/a.zzz"));
                Assert.That(HttpHandlerFactory.ShouldAllow("dir/a/b/c/a.zzz"));

                Assert.That(!HttpHandlerFactory.ShouldAllow("a.json"));
                Assert.That(HttpHandlerFactory.ShouldAllow("jspm_packages/a.json"));
                Assert.That(HttpHandlerFactory.ShouldAllow("jspm_packages/a/b/c/a.json"));
            }
        }
    }
}