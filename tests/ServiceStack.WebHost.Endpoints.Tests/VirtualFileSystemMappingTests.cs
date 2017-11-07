using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Funq;
using NUnit.Framework;
using ServiceStack.IO;
using ServiceStack.Testing;
using ServiceStack.Text;
using ServiceStack.VirtualPath;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class GetMappedFile : IReturn<GetMappedFileResponse>
    {
        public string VirtualPath { get; set; }
    }

    public class GetMappedFileResponse
    {
        public string VirtualPath { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string Contents { get; set; }
    }

    public class FileSystemMappingService : Service
    {
        public object Any(GetMappedFile request)
        {
            var file = base.VirtualFileSources.GetFile(request.VirtualPath);
            return new GetMappedFileResponse
            {
                VirtualPath = request.VirtualPath,
                FileName = file.Name,
                FileSize = file.Length,
                Contents = file.ReadAllText(),
            };
        }
    }

    [TestFixture]
    public class VirtualFileSystemMappingTests
    {
        protected ServiceStackHost appHost;

        class AppHost : AppSelfHostBase
        {
            public AppHost()
                : base(nameof(VirtualFileSystemMappingTests), typeof(FileSystemMappingService).Assembly) { }

            public override void Configure(Container container)
            {
            }

            public override List<IVirtualPathProvider> GetVirtualFileSources()
            {
                var existingSources = base.GetVirtualFileSources();
                existingSources.Add(new FileSystemMapping("vfs1", MapProjectPath("~/App_Data/mount1")));
                existingSources.Add(new FileSystemMapping("vfs2", MapProjectPath("~/App_Data/mount2")));
                return existingSources;
            }
        }

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new AppHost();
            var dirPath = ClearFolders();

            Directory.CreateDirectory(dirPath.AppendPath("mount1", "dir1"));
            File.WriteAllText(dirPath.AppendPath("mount1", "file.txt"), "MOUNT1");
            File.WriteAllText(dirPath.AppendPath("mount1", "dir1", "nested-file.txt"), "NESTED MOUNT1");

            Directory.CreateDirectory(dirPath.AppendPath("mount2", "dir2"));
            File.WriteAllText(dirPath.AppendPath("mount2", "file.txt"), "MOUNT2");
            File.WriteAllText(dirPath.AppendPath("mount2", "dir2", "nested-file.txt"), "NESTED MOUNT2");

            appHost
                .Init()
                .Start(Config.ListeningOn);
        }

        private string ClearFolders()
        {
            var dirPath = appHost.MapProjectPath("~/App_Data");
            if (Directory.Exists(dirPath.AppendPath("mount1")))
                Directory.Delete(dirPath.AppendPath("mount1"), recursive: true);
            if (Directory.Exists(dirPath.AppendPath("mount2")))
                Directory.Delete(dirPath.AppendPath("mount2"), recursive: true);
            return dirPath;
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
            ClearFolders();
        }

        [Test]
        public void Can_resolve_file_from_mapped_path()
        {
            var file1 = appHost.VirtualFileSources.GetFile("vfs1/file.txt");
            var file2 = appHost.VirtualFileSources.GetFile("vfs2/file.txt");

            Assert.That(file1.Name, Is.EqualTo("file.txt"));
            Assert.That(file1.ReadAllText(), Is.EqualTo("MOUNT1"));

            Assert.That(file2.Name, Is.EqualTo("file.txt"));
            Assert.That(file2.ReadAllText(), Is.EqualTo("MOUNT2"));
        }

        [Test]
        public void Can_resolve_nested_file_from_mapped_path()
        {
            var file1 = appHost.VirtualFileSources.GetFile("vfs1/dir1/nested-file.txt");
            var file2 = appHost.VirtualFileSources.GetFile("vfs2/dir2/nested-file.txt");

            Assert.That(file1.Name, Is.EqualTo("nested-file.txt"));
            Assert.That(file1.ReadAllText(), Is.EqualTo("NESTED MOUNT1"));

            Assert.That(file2.Name, Is.EqualTo("nested-file.txt"));
            Assert.That(file2.ReadAllText(), Is.EqualTo("NESTED MOUNT2"));
        }

        [Test]
        public void Can_resolve_mapped_files_from_service()
        {
            var client = new JsonServiceClient(Config.ListeningOn);
            var response = client.Get(new GetMappedFile { VirtualPath = "vfs1/file.txt" });
            Assert.That(response.FileName, Is.EqualTo("file.txt"));
            Assert.That(response.Contents, Is.EqualTo("MOUNT1"));
            Assert.That(response.FileSize, Is.GreaterThan(0));

            response = client.Get(new GetMappedFile { VirtualPath = "vfs2/dir2/nested-file.txt" });
            Assert.That(response.FileName, Is.EqualTo("nested-file.txt"));
            Assert.That(response.Contents, Is.EqualTo("NESTED MOUNT2"));
            Assert.That(response.FileSize, Is.GreaterThan(0));
        }

        [Test]
        public void Can_resolve_mapped_files_directly()
        {
            var url = Config.ListeningOn.AppendPath("vfs1", "file.txt");
            var contents = url.GetStringFromUrl();
            Assert.That(contents, Is.EqualTo("MOUNT1"));

            contents = Config.ListeningOn.AppendPath("vfs2", "dir2", "nested-file.txt").GetStringFromUrl();
            Assert.That(contents, Is.EqualTo("NESTED MOUNT2"));
        }
    }
}