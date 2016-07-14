using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using ServiceStack.IO;
using ServiceStack.Testing;
using ServiceStack.Text;
using ServiceStack.VirtualPath;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class FileSystemVirtualPathProviderTests : VirtualPathProviderTests
    {
        private static string RootDir = "~/App_Data".MapProjectPath();

        public FileSystemVirtualPathProviderTests()
        {
            if (!Directory.Exists(RootDir))
                Directory.CreateDirectory(RootDir);
        }

        public override IVirtualPathProvider GetPathProvider()
        {
            return new FileSystemVirtualPathProvider(appHost, RootDir);
        }
    }

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
            Assert.That(file.ReadAllText(), Is.EqualTo("file")); //can read twice

            Assert.That(file.VirtualPath, Is.EqualTo(filePath));
            Assert.That(file.Name, Is.EqualTo("file.txt"));
            Assert.That(file.Directory.Name, Is.EqualTo("dir"));
            Assert.That(file.Directory.VirtualPath, Is.EqualTo("dir"));
            Assert.That(file.Extension, Is.EqualTo("txt"));

            Assert.That(file.Directory.Name, Is.EqualTo("dir"));

            pathProvider.DeleteFolder("dir");
        }

        [Test]
        public void Does_refresh_LastModified()
        {
            var pathProvider = GetPathProvider();

            var filePath = "dir/file.txt";
            pathProvider.WriteFile(filePath, "file1");

            var file = pathProvider.GetFile(filePath);
            var prevLastModified = file.LastModified;

            file.Refresh();
            Assert.That(file.LastModified, Is.EqualTo(prevLastModified));

            pathProvider.WriteFile(filePath, "file2");
            file.Refresh();

            //Can be too quick and share same modifieddate sometimes, try again with a delay
            if (file.LastModified == prevLastModified)
            {
                Thread.Sleep(1000);
                pathProvider.WriteFile(filePath, "file3");
                file.Refresh();
            }

            Assert.That(file.LastModified, Is.Not.EqualTo(prevLastModified));

            pathProvider.DeleteFolder("dir");
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

            Assert.That(file.Directory.VirtualPath, Is.Null);
            Assert.That(file.Directory.Name, Is.Null.Or.EqualTo("App_Data"));

            pathProvider.DeleteFiles(new[] { "file.txt" });
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

            pathProvider.DeleteFiles(new[] { "file.txt", "/a/file.txt" });
            pathProvider.DeleteFolder("a");
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
            var filePaths = testdir.Files.Map(x => x.VirtualPath);

            Assert.That(filePaths, Is.EquivalentTo(testdirFileNames));

            var fileNames = testdir.Files.Map(x => x.Name);
            Assert.That(fileNames, Is.EquivalentTo(testdirFileNames.Map(x =>
                x.SplitOnLast('/').Last())));

            pathProvider.DeleteFolder("testdir");
        }

        [Test]
        public void Does_resolve_nested_files_and_folders()
        {
            var pathProvider = GetPathProvider();

            pathProvider.DeleteFiles(pathProvider.GetAllFiles());

            var allFilePaths = new[] {
                "testfile.txt",
                "a/testfile-a1.txt",
                "a/testfile-a2.txt",
                "a/b/testfile-ab1.txt",
                "a/b/testfile-ab2.txt",
                "a/b/c/testfile-abc1.txt",
                "a/b/c/testfile-abc2.txt",
                "a/d/testfile-ad1.txt",
                "e/testfile-e1.txt",
            };

            allFilePaths.Each(x => pathProvider.WriteFile(x, x.SplitOnLast('.').First().SplitOnLast('/').Last()));

            Assert.That(allFilePaths.All(x => pathProvider.IsFile(x)));
            Assert.That(new[] { "a", "a/b", "a/b/c", "a/d", "e" }.All(x => pathProvider.IsDirectory(x)));

            Assert.That(!pathProvider.IsFile("notfound.txt"));
            Assert.That(!pathProvider.IsFile("a/notfound.txt"));
            Assert.That(!pathProvider.IsDirectory("f"));
            Assert.That(!pathProvider.IsDirectory("a/f"));
            Assert.That(!pathProvider.IsDirectory("testfile.txt"));
            Assert.That(!pathProvider.IsDirectory("a/testfile-a1.txt"));

            Assert.That(pathProvider.DirectoryExists("a"));
            Assert.That(pathProvider.GetDirectory("a"), Is.Not.Null);
            Assert.That(pathProvider.DirectoryExists("a/b"));
            Assert.That(pathProvider.GetDirectory("a/b"), Is.Not.Null);
            Assert.That(!pathProvider.DirectoryExists("b"));
            Assert.That(pathProvider.GetDirectory("b"), Is.Null);
            Assert.That(!pathProvider.DirectoryExists("a/c"));
            Assert.That(pathProvider.GetDirectory("a/c"), Is.Null);

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

            var dirs = pathProvider.RootDirectory.Directories.Map(x => x.VirtualPath);
            Assert.That(dirs, Is.EquivalentTo(new[] { "a", "e" }));

            var rootDirFiles = pathProvider.RootDirectory.GetAllMatchingFiles("*", 1).Map(x => x.VirtualPath);
            Assert.That(rootDirFiles, Is.EquivalentTo(new[] { "testfile.txt" }));

            var allFiles = pathProvider.GetAllMatchingFiles("*").Map(x => x.VirtualPath);
            Assert.That(allFiles, Is.EquivalentTo(allFilePaths));

            allFiles = pathProvider.GetAllFiles().Map(x => x.VirtualPath);
            Assert.That(allFiles, Is.EquivalentTo(allFilePaths));

            pathProvider.DeleteFile("testfile.txt");
            pathProvider.DeleteFolder("a");
            pathProvider.DeleteFolder("e");

            Assert.That(pathProvider.GetAllFiles().ToList().Count, Is.EqualTo(0));
        }

        [Test]
        public void Does_append_to_file()
        {
            var pathProvider = GetPathProvider();
            pathProvider.DeleteFile("original.txt");
            pathProvider.WriteFile("original.txt", "original\n");

            pathProvider.AppendFile("original.txt", "New Line1\n");
            pathProvider.AppendFile("original.txt", "New Line2\n");

            var contents = pathProvider.GetFile("original.txt").ReadAllText();
            Assert.That(contents, Is.EqualTo("original\nNew Line1\nNew Line2\n"));
        }

        [Test]
        public void Does_append_to_file_bytes()
        {
            var pathProvider = GetPathProvider();
            pathProvider.DeleteFile("original.bin");
            pathProvider.WriteFile("original.bin", "original\n".ToUtf8Bytes());

            pathProvider.AppendFile("original.bin", "New Line1\n".ToUtf8Bytes());
            pathProvider.AppendFile("original.bin", "New Line2\n".ToUtf8Bytes());

            var contents = pathProvider.GetFile("original.bin").ReadAllBytes();
            Assert.That(contents, Is.EquivalentTo("original\nNew Line1\nNew Line2\n".ToUtf8Bytes()));
        }

        public void AssertContents(IVirtualDirectory dir,
            string[] expectedFilePaths, string[] expectedDirPaths)
        {
            var filePaths = dir.Files.Map(x => x.VirtualPath);
            Assert.That(filePaths, Is.EquivalentTo(expectedFilePaths));

            var fileNames = dir.Files.Map(x => x.Name);
            Assert.That(fileNames, Is.EquivalentTo(expectedFilePaths.Map(x =>
                x.SplitOnLast('/').Last())));

            var dirPaths = dir.Directories.Map(x => x.VirtualPath);
            Assert.That(dirPaths, Is.EquivalentTo(expectedDirPaths));

            var dirNames = dir.Directories.Map(x => x.Name);
            Assert.That(dirNames, Is.EquivalentTo(expectedDirPaths.Map(x =>
                x.SplitOnLast('/').Last())));
        }
    }
}