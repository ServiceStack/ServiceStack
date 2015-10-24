using NUnit.Framework;
using ServiceStack.IO;
using ServiceStack.Testing;
using ServiceStack.VirtualPath;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class InMemoryVirtualPathProviderTests : VirtualPathProviderTests
    {
        public override IVirtualPathProvider GetPathProvider()
        {
            return new InMemoryVirtualPathProvider(appHost);
        }
    }

    [TestFixture]
    public abstract class VirtualPathProviderTests
    {
        public abstract IVirtualPathProvider GetPathProvider();

        protected ServiceStackHost appHost;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new BasicAppHost()
                .Init();
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Can_create_file()
        {
            var pathProvider = GetPathProvider();

            var filePath = "dir/file.txt";
            pathProvider.WriteFile(filePath, "file");

            var file = pathProvider.GetFile(filePath);

            Assert.That(file.ReadAllText(), Is.EqualTo("file"));
            Assert.That(file.Name, Is.EqualTo(filePath));
            Assert.That(file.Extension, Is.EqualTo("txt"));

            Assert.That(file.Directory.Name, Is.EqualTo("dir"));
        }

        [Test]
        public void Can_create_file_from_root()
        {
            var pathProvider = GetPathProvider();

            var filePath = "file.txt";
            pathProvider.WriteFile(filePath, "file");

            var file = pathProvider.GetFile(filePath);

            Assert.That(file.ReadAllText(), Is.EqualTo("file"));
            Assert.That(file.Name, Is.EqualTo(filePath));
            Assert.That(file.Extension, Is.EqualTo("txt"));

            Assert.That(file.Directory.Name, Is.Null);
        }

        [Test]
        public void Does_override_existing_file()
        {
            var pathProvider = GetPathProvider();

            pathProvider.WriteFile("file.txt", "original");
            pathProvider.WriteFile("file.txt", "updated");
            Assert.That(pathProvider.GetFile("file.txt").ReadAllText(), Is.EqualTo("updated"));

            pathProvider.WriteFile("/a/file.txt", "original");
            pathProvider.WriteFile("/a/file.txt", "updated");
            Assert.That(pathProvider.GetFile("/a/file.txt").ReadAllText(), Is.EqualTo("updated"));
        }

        [Test]
        public void Can_view_files_in_Directory()
        {
            var pathProvider = GetPathProvider();

            var testdirFileNames = new[]
            {
                "testdir/a.txt",
                "testdir/b.txt",
                "testdir/c.txt",
            };

            testdirFileNames.Each(x => pathProvider.WriteFile(x, "textfile"));

            var testdir = pathProvider.GetDirectory("testdir");
            var fileNames = testdir.Files.Map(x => x.Name);

            Assert.That(fileNames, Is.EquivalentTo(testdirFileNames));
        }

        [Test]
        public void Does_resolve_nested_files_and_folders()
        {
            var pathProvider = GetPathProvider();

            pathProvider.WriteFile("testfile.txt", "testfile");
            pathProvider.WriteFile("a/testfile-a1.txt", "testfile-a1");
            pathProvider.WriteFile("a/testfile-a2.txt", "testfile-a2");
            pathProvider.WriteFile("a/b/testfile-ab1.txt", "testfile-ab1");
            pathProvider.WriteFile("a/b/testfile-ab2.txt", "testfile-ab2");
            pathProvider.WriteFile("a/b/c/testfile-abc1.txt", "testfile-abc1");
            pathProvider.WriteFile("a/b/c/testfile-abc2.txt", "testfile-abc2");
            pathProvider.WriteFile("a/d/testfile-ad1.txt", "testfile-ad1");
            pathProvider.WriteFile("e/testfile-e1.txt", "testfile-e1");

            AssertContents(pathProvider.RootDirectory, new[] {
                    "testfile.txt",
                }, new[] {
                    "a",
                    "e"
                });

            AssertContents(pathProvider.GetDirectory("a"), new[] {
                    "a/testfile-a1.txt",
                    "a/testfile-a2.txt",
                }, new[] {
                    "a/b",
                    "a/d"
                });

            AssertContents(pathProvider.GetDirectory("a/b"), new[] {
                    "a/b/testfile-ab1.txt",
                    "a/b/testfile-ab2.txt",
                }, new[] {
                    "a/b/c"
                });

            AssertContents(pathProvider.GetDirectory("a").GetDirectory("b"), new[] {
                    "a/b/testfile-ab1.txt",
                    "a/b/testfile-ab2.txt",
                }, new[] {
                    "a/b/c"
                });

            AssertContents(pathProvider.GetDirectory("a/b/c"), new[] {
                    "a/b/c/testfile-abc1.txt",
                    "a/b/c/testfile-abc2.txt",
                }, new string[0]);

            AssertContents(pathProvider.GetDirectory("a/d"), new[] {
                    "a/d/testfile-ad1.txt",
                }, new string[0]);

            AssertContents(pathProvider.GetDirectory("e"), new[] {
                    "e/testfile-e1.txt",
                }, new string[0]);

            Assert.That(pathProvider.GetFile("a/b/c/testfile-abc1.txt").ReadAllText(), Is.EqualTo("testfile-abc1"));
            Assert.That(pathProvider.GetDirectory("a").GetFile("b/c/testfile-abc1.txt").ReadAllText(), Is.EqualTo("testfile-abc1"));
            Assert.That(pathProvider.GetDirectory("a/b").GetFile("c/testfile-abc1.txt").ReadAllText(), Is.EqualTo("testfile-abc1"));
            Assert.That(pathProvider.GetDirectory("a").GetDirectory("b").GetDirectory("c").GetFile("testfile-abc1.txt").ReadAllText(), Is.EqualTo("testfile-abc1"));
        }

        public void AssertContents(IVirtualDirectory dir, 
            string[] expectedFileNames, string[] expectedDirNames)
        {
            var fileNames = dir.Files.Map(x => x.Name);
            Assert.That(fileNames, Is.EquivalentTo(expectedFileNames));

            var dirNames = dir.Directories.Map(x => x.Name);
            Assert.That(dirNames, Is.EquivalentTo(expectedDirNames));
        }
    }
}