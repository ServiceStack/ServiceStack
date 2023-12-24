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
                Assert.That(VirtualFilesFeature.ShouldAllow("a.js"));
                Assert.That(VirtualFilesFeature.ShouldAllow("a.aaa"));
                Assert.That(VirtualFilesFeature.ShouldAllow("dir/a/b/c/a.aaa"));
                Assert.That(!VirtualFilesFeature.ShouldAllow("a.zzz"));
                Assert.That(VirtualFilesFeature.ShouldAllow("dir/a.zzz"));
                Assert.That(VirtualFilesFeature.ShouldAllow("dir/a/b/c/a.zzz"));

                Assert.That(!VirtualFilesFeature.ShouldAllow("a.json"));
                Assert.That(VirtualFilesFeature.ShouldAllow("jspm_packages/a.json"));
                Assert.That(VirtualFilesFeature.ShouldAllow("jspm_packages/a/b/c/a.json"));
            }
        }
    }
}