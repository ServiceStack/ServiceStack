using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.IO;
using ServiceStack.Testing;
using ServiceStack.Text;
using ServiceStack.VirtualPath;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class FileSystemVirtualPathProviderTests : AppendVirtualFilesTests
    {
        private static string RootDir = "~/App_Data/files".MapProjectPath();

        public FileSystemVirtualPathProviderTests()
        {
            if (Directory.Exists(RootDir))
                Directory.Delete(RootDir, recursive:true);
                
            Directory.CreateDirectory(RootDir);
        }

        public override IVirtualPathProvider GetPathProvider()
        {
            return new FileSystemVirtualFiles(RootDir);
        }
    }

    public class MemoryVirtualFilesTests : AppendVirtualFilesTests
    {
        public override IVirtualPathProvider GetPathProvider()
        {
            return new MemoryVirtualFiles();
        }
    }

    [Ignore("Integration Tests")]
    public class GistVirtualFilesTests : VirtualPathProviderTests
    {
        public static readonly string GistId = "a9cfcdced0002e82be20ea6314fb41d6";
        public static readonly string AccessToken = Environment.GetEnvironmentVariable("GITHUB_GIST_TOKEN");

        public override IVirtualPathProvider GetPathProvider()
        {
            return new GistVirtualFiles(GistId, AccessToken);
        }
    }

    public abstract class AppendVirtualFilesTests : VirtualPathProviderTests
    {
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
            
            pathProvider.DeleteFile("original.txt");
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
            
            pathProvider.DeleteFile("original.bin");
        }
    }

    [TestFixture]
    public abstract class VirtualPathProviderTests
    {
        public abstract IVirtualPathProvider GetPathProvider();

        protected ServiceStackHost appHost;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new BasicAppHost()
                .Init();
        }

        [OneTimeTearDown]
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

            //Can be too quick and share same modified date sometimes, try again with a delay
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
            Assert.That(file.Directory.Name, Is.Null.Or.EqualTo("files"));

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

            var to = new Dictionary<string, string>();
            testdirFileNames.Each(x => to[x] = "textfile");
            pathProvider.WriteFiles(to);

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

            var to = new Dictionary<string, string>();
            allFilePaths.Each(x => to[x] = x.SplitOnLast('.').First().SplitOnLast('/').Last());
            pathProvider.WriteFiles(to);

            Assert.That(allFilePaths.All(x => pathProvider.IsFile(x)));
            Assert.That(new[] { "a", "a/b", "a/b/c", "a/d", "e" }.All(x => pathProvider.IsDirectory(x)));

            Assert.That(!pathProvider.IsFile("notfound.txt"));
            Assert.That(!pathProvider.IsFile("a/notfound.txt"));
            Assert.That(!pathProvider.IsDirectory("f"));
            Assert.That(!pathProvider.IsDirectory("a/f"));
            Assert.That(!pathProvider.IsDirectory("testfile.txt"));
            Assert.That(!pathProvider.IsDirectory("a/testfile-a1.txt"));

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
                }, Array.Empty<string>());

            AssertContents(pathProvider.GetDirectory("a/d"), new[] {
                    "a/d/testfile-ad1.txt",
                }, Array.Empty<string>());

            AssertContents(pathProvider.GetDirectory("e"), new[] {
                    "e/testfile-e1.txt",
                }, Array.Empty<string>());

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

            Assert.That(pathProvider.DirectoryExists("a"));
            Assert.That(!pathProvider.DirectoryExists("f"));
            Assert.That(!pathProvider.GetDirectory("a/b/c").IsRoot);
            Assert.That(!pathProvider.GetDirectory("a/b").IsRoot);
            Assert.That(!pathProvider.GetDirectory("a").IsRoot);
            Assert.That(pathProvider.GetDirectory("").IsRoot);

            pathProvider.DeleteFile("testfile.txt");
            pathProvider.DeleteFolder("a");
            pathProvider.DeleteFolder("e");

            Assert.That(pathProvider.GetAllFiles().ToList().Count, Is.EqualTo(0));
        }

        [Test]
        public void Can_GetAllMatchingFiles_in_nested_directories()
        {
            var pathProvider = GetPathProvider();

            var allFilePaths = new[] {
                "a/b/c/testfile-abc1.txt",
                "a/b/c/d/e/f/g/testfile-abcdefg1.txt",
            };

            var to = new Dictionary<string, string>();
            allFilePaths.Each(x => to[x] = x.SplitOnLast('.').First().SplitOnLast('/').Last());
            pathProvider.WriteFiles(to);

            Assert.That(pathProvider.GetDirectory("a/b/c").GetAllMatchingFiles("testfile-abc1.txt").Count(), Is.EqualTo(1));
            Assert.That(pathProvider.GetDirectory("a/b").GetAllMatchingFiles("testfile-abc1.txt").Count(), Is.EqualTo(1));
            Assert.That(pathProvider.GetDirectory("a").GetAllMatchingFiles("testfile-abc1.txt").Count(), Is.EqualTo(1));

            Assert.That(pathProvider.GetDirectory("a/b/c/d/e/f/g").GetAllMatchingFiles("testfile-abcdefg1.txt").Count(), Is.EqualTo(1));
            Assert.That(pathProvider.GetDirectory("a/b/c/d/e/f").GetAllMatchingFiles("testfile-abcdefg1.txt").Count(), Is.EqualTo(1));
            Assert.That(pathProvider.GetDirectory("a/b/c/d/e").GetAllMatchingFiles("testfile-abcdefg1.txt").Count(), Is.EqualTo(1));
            Assert.That(pathProvider.GetDirectory("a/b/c/d").GetAllMatchingFiles("testfile-abcdefg1.txt").Count(), Is.EqualTo(1));
            Assert.That(pathProvider.GetDirectory("a/b/c").GetAllMatchingFiles("testfile-abcdefg1.txt").Count(), Is.EqualTo(1));
            Assert.That(pathProvider.GetDirectory("a/b").GetAllMatchingFiles("testfile-abcdefg1.txt").Count(), Is.EqualTo(1));
            Assert.That(pathProvider.GetDirectory("a").GetAllMatchingFiles("testfile-abcdefg1.txt").Count(), Is.EqualTo(1));
        }

        [Test]
        public void Does_create_file_in_nested_folders_with_correct_parent_directories()
        {
            var vfs = GetPathProvider();
            
            vfs.WriteFile("a/b/c/file.txt", "file");
            var file = vfs.GetFile("a/b/c/file.txt");
            Assert.That(file != null);

            Assert.That(file.Directory.VirtualPath, Is.EqualTo("a/b/c"));
            Assert.That(file.Directory.Name, Is.EqualTo("c"));
            Assert.That(file.Directory.ParentDirectory.VirtualPath, Is.EqualTo("a/b"));
            Assert.That(file.Directory.ParentDirectory.Name, Is.EqualTo("b"));
            Assert.That(file.Directory.ParentDirectory.ParentDirectory.VirtualPath, Is.EqualTo("a"));
            Assert.That(file.Directory.ParentDirectory.ParentDirectory.Name, Is.EqualTo("a"));
            Assert.That(file.Directory.ParentDirectory.ParentDirectory.ParentDirectory.IsRoot);
            Assert.That(vfs.RootDirectory.GetDirectories().Any(x => x.Name == "a"));
            
            vfs.DeleteFile("a/b/c/file.txt");
        }

        [Test]
        public void Does_write_binary_file()
        {
            var pathProvider = GetPathProvider();

            var bytes = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            
            pathProvider.WriteFile("original.bin", bytes);
            pathProvider.WriteFile("a/b/c/original.bin", bytes);

            var contents = pathProvider.GetFile("original.bin").ReadAllBytes();
            Assert.That(contents, Is.EquivalentTo(bytes));

            contents = pathProvider.GetFile("a/b/c/original.bin").ReadAllBytes();
            Assert.That(contents, Is.EquivalentTo(bytes));

            pathProvider.DeleteFiles(new[]{ "original.bin", "a/b/c/original.bin" });
        }

        [Test]
        public void GetContents_of_Binary_File_returns_ReadOnlyMemory_byte()
        {
            var pathProvider = GetPathProvider();

            var bytes = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            
            pathProvider.WriteFile("original.bin", new ReadOnlyMemory<byte>(bytes));
            
            var contents = (ReadOnlyMemory<byte>) pathProvider.GetFile("original.bin").GetContents();
            
            Assert.That(contents.Span.SequenceEqual(bytes.AsSpan()));
        }

        [Test]
        public void GetContents_of_Text_File_returns_ReadOnlyMemory_char()
        {
            var pathProvider = GetPathProvider();

            var text = "abcdef";
            
            pathProvider.WriteFile("original.txt", text.AsMemory());
            
            var contents = (ReadOnlyMemory<char>) pathProvider.GetFile("original.txt").GetContents();
            
            Assert.That(contents.Span.SequenceEqual(text.AsSpan()));
        }

        byte[] ReadAndReset(MemoryStream ms)
        {
            var ret = ms.ToArray();
            ms.SetLength(0);
            return ret;
        }
        string ReadAsStringAndReset(MemoryStream ms) => ReadAndReset(ms).FromUtf8Bytes();

        [Test]
        public async Task Can_WritePartialToAsync()
        {
            var pathProvider = GetPathProvider();
            var customText = "1234567890";
            var customTextBytes = customText.ToUtf8Bytes();

            pathProvider.WriteFile("original.txt", customTextBytes);
            var file = pathProvider.GetFile("original.txt");

            var ms = new MemoryStream();

            // Ranges (follows HTTP Ranges) are inclusive 
            await file.WritePartialToAsync(ms, 0, customTextBytes.Length - 1);
            Assert.That(ReadAndReset(ms), Is.EquivalentTo(customTextBytes));
            
            await file.WritePartialToAsync(ms, 0, customTextBytes.Length + 1);
            Assert.That(ReadAndReset(ms), Is.EquivalentTo(customTextBytes));
            
            await file.WritePartialToAsync(ms, 0, 2);
            Assert.That(ReadAsStringAndReset(ms), Is.EquivalentTo("123"));
            
            await file.WritePartialToAsync(ms, 4, int.MaxValue);
            Assert.That(ReadAsStringAndReset(ms), Is.EquivalentTo("567890"));
            
            await file.WritePartialToAsync(ms, 3, 5);
            Assert.That(ReadAsStringAndReset(ms), Is.EquivalentTo("456"));
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