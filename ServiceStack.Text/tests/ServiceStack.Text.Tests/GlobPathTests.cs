using NUnit.Framework;
using ServiceStack;
using ServiceStack.Text;

namespace ServiceStack.Text.Tests
{
    [TestFixture]
    public class GlobPathTests
    {
        [Test]
        public void Does_validate_GlobPaths()
        {
            Assert.That(!"/dir/a/file.txt".GlobPath(""));
            Assert.That(!"".GlobPath("*.txt"));
            Assert.That("file.txt".GlobPath("file.txt"));
            Assert.That(!"file.txt".GlobPath("file.json"));
            Assert.That(!"file.txt".GlobPath("*.json"));
            Assert.That("file.txt".GlobPath("*.txt"));

            Assert.That("/dir/a/file.txt".GlobPath("/dir/a/file.txt"));
            Assert.That("dir\\a/file.txt".GlobPath("/dir/a/file.txt"));
            Assert.That("/dir/a/file.txt".GlobPath("dir\\a/file.txt"));
            Assert.That(!"/dir/a/file.txt".GlobPath("/dir/b/file.txt"));
            Assert.That(!"/dir/a/file.txt".GlobPath("/dir/a/file2.txt"));
            Assert.That(!"/dir/a/file.txt".GlobPath("dir\\a/file2.txt"));
            Assert.That(!"/Dir/a/file.txt".GlobPath("/dir/a/file2.txt"));
            Assert.That(!"/dir1/a/file.txt".GlobPath("/dir2/a/file2.txt"));
            Assert.That(!"/dir/a/file.txt".GlobPath("/dir/a/file.json"));
            Assert.That(!"/dir/a/file.txt".GlobPath("/dir/a/file2.txt"));
            Assert.That(!"/dir/a/file.txt".GlobPath("/dir/a/file2.*"));
            Assert.That(!"/dir/a/file.txt".GlobPath("/dir/a/*.json"));

            Assert.That("dir/a/file.txt".GlobPath("dir/*/file.txt"));
            Assert.That(!"dir/file.txt".GlobPath("dir/*/file.txt"));
            Assert.That(!"dir/a/b/file.txt".GlobPath("dir/*/file.txt"));
            Assert.That("dir/ab/file.txt".GlobPath("dir/a*/file.txt"));
            Assert.That("dir/abc/file.txt".GlobPath("dir/a*/file.txt"));
            Assert.That("dir/ab/file.txt".GlobPath("dir/a?/file.txt"));
            Assert.That(!"dir/abc/file.txt".GlobPath("dir/a?/file.txt"));
            Assert.That("dir/abc/file.txt".GlobPath("dir/a?c/file.txt"));

            Assert.That("dir/file.txt".GlobPath("dir/**/file.txt"));
            Assert.That(!"Dir/file.txt".GlobPath("dir/**/file.txt"));
            Assert.That("dir/a/file.txt".GlobPath("dir/**/file.txt"));
            Assert.That("dir/a/b/file.txt".GlobPath("dir/**/file.txt"));
            Assert.That("dir/a/b/c/d/e/file.txt".GlobPath("dir/**/file.txt"));
            Assert.That("dir/a/b/c/d/e/file.json".GlobPath("dir/**/*.json"));

            Assert.That("/jspm_packages/npm/zone.js@0.6.26.json".GlobPath("jspm_packages/**/*.json"));
            Assert.That("/.well-known/acme-challenge/XzF9VXFuw4UMBVdiX2jDj2vykjrvEsQR8AZ8kJiaBdk".GlobPath(".well-known/**/*"));
        }
    }
}