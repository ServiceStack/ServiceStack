using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ServiceStack.IO;
using ServiceStack.Text;
using ServiceStack.VirtualPath;

namespace ServiceStack.WebHost.Endpoints.Tests;

[TestFixture]
public class VirtualPathModernizationTests
{
    [Test]
    public void IsPathSafe_Prevents_Prefix_Directory_Traversal()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "sstest_vfs_" + Guid.NewGuid().ToString("N"));
        var appDir = Path.Combine(tempDir, "app");
        var appSecretDir = Path.Combine(tempDir, "app_secret");

        try
        {
            Directory.CreateDirectory(appDir);
            Directory.CreateDirectory(appSecretDir);

            // Valid relative paths within base
            Assert.That(FileSystemVirtualFiles.IsPathSafe(appDir, "file.txt"), Is.True);
            Assert.That(FileSystemVirtualFiles.IsPathSafe(appDir, "sub/file.txt"), Is.True);
            Assert.That(FileSystemVirtualFiles.IsPathSafe(appDir, "./sub/file.txt"), Is.True);

            // Path traversal attempting to reach sibling directory starting with same prefix ("app_secret")
            Assert.That(FileSystemVirtualFiles.IsPathSafe(appDir, "../app_secret/secret.txt"), Is.False);
            Assert.That(FileSystemVirtualFiles.IsPathSafe(appDir, "../../"), Is.False);

            // Null inputs
            Assert.That(FileSystemVirtualFiles.IsPathSafe(null, "file.txt"), Is.False);
            Assert.That(FileSystemVirtualFiles.IsPathSafe(appDir, null), Is.False);
            Assert.That(FileSystemVirtualFiles.IsPathSafe("", "file.txt"), Is.False);
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
            catch { /* ignored */ }
        }
    }

    [Test]
    public void FileSystemMapping_GetRealVirtualPath_Prevents_Prefix_Collisions()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "sstest_fsm_" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(tempDir);
            var mapping = new FileSystemMapping("docs", tempDir);

            // Exact match returns empty string (represents root of mapped folder)
            Assert.That(mapping.GetRealVirtualPath("docs"), Is.EqualTo(string.Empty));
            Assert.That(mapping.GetRealVirtualPath("/docs"), Is.EqualTo(string.Empty));

            // Valid subpaths with separator
            Assert.That(mapping.GetRealVirtualPath("docs/readme.md"), Is.EqualTo("readme.md"));
            Assert.That(mapping.GetRealVirtualPath("/docs/sub/readme.md"), Is.EqualTo("sub/readme.md"));

            // Sibling prefix collisions MUST return null (not match)
            Assert.That(mapping.GetRealVirtualPath("documentation/readme.md"), Is.Null);
            Assert.That(mapping.GetRealVirtualPath("/docstrings/file.txt"), Is.Null);
            Assert.That(mapping.GetRealVirtualPath("docs2/file.txt"), Is.Null);

            // Null/empty handling
            Assert.That(mapping.GetRealVirtualPath(null), Is.Null);
            Assert.That(mapping.GetRealVirtualPath(""), Is.Null);
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
            catch { /* ignored */ }
        }
    }

    [Test]
    public void MultiVirtualDirectory_ParentDirectory_DoesNotThrow_InvalidCastException()
    {
        var mem1 = new MemoryVirtualFiles();
        mem1.WriteFile("parent/child/test1.txt", "content1");
        mem1.WriteFile("parent/sibling.txt", "sibling"); // file in parent directory

        var mem2 = new MemoryVirtualFiles();
        mem2.WriteFile("parent/child/test2.txt", "content2");

        var dir1 = mem1.GetDirectory("parent/child");
        var dir2 = mem2.GetDirectory("parent/child");

        var multiDir = new MultiVirtualDirectory(new[] { dir1, dir2 });

        // Resolving ParentDirectory must not throw InvalidCastException when parent has files
        var parent = multiDir.ParentDirectory;
        Assert.That(parent, Is.Not.Null);
        Assert.That(parent.Name, Is.EqualTo("parent"));
    }

    [Test]
    public void MultiVirtualDirectory_Properties_DoNotThrow_When_Empty()
    {
        var mem1 = new MemoryVirtualFiles();
        var mem2 = new MemoryVirtualFiles();

        var dir1 = new InMemoryVirtualDirectory(mem1, "empty_dir");
        var dir2 = new InMemoryVirtualDirectory(mem2, "empty_dir");

        var multiDir = new MultiVirtualDirectory(new[] { dir1, dir2 });

        // Previously this.First() threw InvalidOperationException on empty directories
        Assert.That(multiDir.Name, Is.EqualTo("empty_dir"));
        Assert.That(multiDir.VirtualPath, Is.EqualTo("empty_dir"));
        Assert.That(multiDir.IsDirectory, Is.True);
        Assert.That(multiDir.Files.Count(), Is.EqualTo(0));
        Assert.That(multiDir.Directories.Count(), Is.EqualTo(0));
    }

    [Test]
    public void MultiVirtualDirectory_GetFile_Preserves_Stack_Across_Providers()
    {
        var mem1 = new MemoryVirtualFiles();
        mem1.WriteFile("other/file.txt", "other");

        var mem2 = new MemoryVirtualFiles();
        mem2.WriteFile("folder/sub/target.txt", "target_content");

        var multiDir = new MultiVirtualDirectory(new[] { mem1.RootDirectory, mem2.RootDirectory });

        var stack = "folder/sub/target.txt".TokenizeVirtualPath("/");
        var file = multiDir.GetFile(stack);

        Assert.That(file, Is.Not.Null);
        Assert.That(file.ReadAllText(), Is.EqualTo("target_content"));
    }

    class CustomContentsVirtualFile : AbstractVirtualFileBase
    {
        private readonly byte[] data;

        public CustomContentsVirtualFile(IVirtualPathProvider owningProvider, IVirtualDirectory directory, byte[] data)
            : base(owningProvider, directory)
        {
            this.data = data;
        }

        public override string Name => "custom.bin";
        public override DateTime LastModified => DateTime.UtcNow;
        public override long Length => data.Length;

        public override Stream OpenRead() => new MemoryStream(data);

        // Returns an unexpected object type (e.g. integer or custom model)
        public override object GetContents() => 12345;
    }

    [Test]
    public void AbstractVirtualFileBase_ReadAllBytes_DoesNotRecursivelyOverflow()
    {
        var mem = new MemoryVirtualFiles();
        var expectedBytes = new byte[] { 1, 2, 3, 4, 5 };
        var customFile = new CustomContentsVirtualFile(mem, mem.RootDirectory, expectedBytes);

        // Previously, if GetContents returned an unhandled type, ReadAllBytes() called itself recursively indefinitely.
        var bytes = customFile.ReadAllBytes();
        Assert.That(bytes, Is.EqualTo(expectedBytes));
    }

    [Test]
    public void AbstractVirtualDirectoryBase_GetHashCode_DoesNotThrow_On_Root()
    {
        var mem = new MemoryVirtualFiles();
        var root = mem.RootDirectory;

        // Root directory has VirtualPath == null
        Assert.That(root.VirtualPath, Is.Null);
        Assert.DoesNotThrow(() =>
        {
            var hash = root.GetHashCode();
            Assert.That(hash, Is.EqualTo(0));
        });
    }

    [Test]
    public void VirtualFileExtensions_ShouldSkipPath_Handles_Nulls_Safely()
    {
        Assert.That(((IVirtualNode)null).ShouldSkipPath(), Is.False);

        var mem = new MemoryVirtualFiles();
        Assert.That(mem.RootDirectory.ShouldSkipPath(), Is.False);

        AbstractVirtualFileBase.ScanSkipPaths.Add("node_modules");
        AbstractVirtualFileBase.ScanSkipPaths.Add(null);
        AbstractVirtualFileBase.ScanSkipPaths.Add("");

        try
        {
            mem.WriteFile("node_modules/pkg/index.js", "content");
            var file = mem.GetFile("node_modules/pkg/index.js");
            Assert.That(file.ShouldSkipPath(), Is.True);

            mem.WriteFile("src/app.js", "content");
            var safeFile = mem.GetFile("src/app.js");
            Assert.That(safeFile.ShouldSkipPath(), Is.False);
        }
        finally
        {
            AbstractVirtualFileBase.ScanSkipPaths.Clear();
        }
    }

    [Test]
    public void InMemoryVirtualDirectory_GetEnumerator_Enumerates_Nodes()
    {
        var mem = new MemoryVirtualFiles();
        mem.WriteFile("dir/file1.txt", "content1");
        mem.WriteFile("dir/sub/file2.txt", "content2");

        var dir = mem.GetDirectory("dir");
        Assert.That(dir, Is.Not.Null);

        // Must not throw NotImplementedException
        var nodes = dir.ToList();
        Assert.That(nodes.Count, Is.GreaterThan(0));
        Assert.That(nodes.Any(x => x.Name == "file1.txt"), Is.True);
        Assert.That(nodes.Any(x => x.Name == "sub"), Is.True);
    }

    [Test]
    public void MemoryVirtualFiles_ThreadSafe_Clear_And_Null_DeleteFiles()
    {
        var mem = new MemoryVirtualFiles();
        mem.WriteFile("a.txt", "content");
        mem.WriteFile("b.txt", "content");

        Assert.DoesNotThrow(() => mem.DeleteFiles(null));
        Assert.DoesNotThrow(() => mem.DeleteFolder(null));

        Assert.That(mem.Files.Count, Is.EqualTo(2));
        mem.Clear();
        Assert.That(mem.Files.Count, Is.EqualTo(0));
    }

    [Test]
    public void VirtualPathUtils_Defensive_Guards()
    {
        Assert.That(VirtualPathUtils.SafeFileName(null), Is.EqualTo(string.Empty));
        Assert.That(VirtualPathUtils.IsValidFilePath(null), Is.False);
        Assert.That(VirtualPathUtils.IsValidFileName(null), Is.False);
        Assert.That(VirtualPathUtils.GetDefaultDocument(null, null), Is.Null);
        Assert.That(VirtualPathUtils.ReadAllBytes((IVirtualFile)null), Is.EqualTo(TypeConstants.EmptyByteArray));
        Assert.That(VirtualPathUtils.GetVirtualNode(null, "path"), Is.Null);

        var stack = "a/b/c".TokenizeVirtualPath((string)null);
        Assert.That(stack.Count, Is.EqualTo(3));
    }

    [Test]
    public void VirtualFilesFeature_Handles_Null_AppHost_And_Null_Requests()
    {
        var feature = new VirtualFilesFeature();
        Assert.DoesNotThrow(() => feature.Register(null));
        Assert.That(feature.GetHandler(null), Is.Null);

        // Path filtering without initialized host config
        Assert.That(VirtualFilesFeature.ShouldAllow(null), Is.True);
        Assert.That(VirtualFilesFeature.ShouldAllow("/"), Is.True);
    }

    [Test]
    public void VirtualFilesExtensions_AssertWritable_Guards_Null_PathProvider()
    {
        IVirtualPathProvider nullProvider = null;
        Assert.Throws<ArgumentNullException>(() => nullProvider.WriteFile("test.txt", "data"));
        Assert.Throws<ArgumentNullException>(() => nullProvider.DeleteFile("test.txt"));
        Assert.Throws<ArgumentNullException>(() => nullProvider.AppendFile("test.txt", "data"));
    }

    [Test]
    public void AbstractVirtualPathProviderBase_CombineVirtualPath_Normalizes_Separators()
    {
        var mem = new MemoryVirtualFiles();
        Assert.That(mem.CombineVirtualPath("a/", "/b"), Is.EqualTo("a/b"));
        Assert.That(mem.CombineVirtualPath("a", "b"), Is.EqualTo("a/b"));
        Assert.That(mem.CombineVirtualPath("", "b"), Is.EqualTo("b"));
        Assert.That(mem.CombineVirtualPath("a", ""), Is.EqualTo("a"));
    }
}
