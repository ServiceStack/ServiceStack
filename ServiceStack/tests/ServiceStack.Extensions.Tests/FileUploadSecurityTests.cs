using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.IO;
using ServiceStack.Testing;
using ServiceStack.Web;

namespace ServiceStack.Extensions.Tests;

[TestFixture]
public class FileSystemUploadSecurityTests
{
    private string scratch;
    private string root;
    private FileSystemVirtualFiles files;

    [SetUp]
    public void SetUp()
    {
        scratch = Path.Combine(Path.GetTempPath(), "ss-upload-security-" + Guid.NewGuid().ToString("N"));
        root = Path.Combine(scratch, "uploads");
        Directory.CreateDirectory(root);
        files = new FileSystemVirtualFiles(root);
        File.WriteAllText(Path.Combine(scratch, "victim.txt"), "original");
    }

    [TearDown]
    public void TearDown() => Directory.Delete(scratch, true);

    private static readonly string[] Operations = {
        "write", "write-stream", "write-async", "append", "append-stream", "delete", "delete-files", "delete-folder"
    };

    public static IEnumerable<TestCaseData> TraversalCases()
    {
        foreach (var operation in Operations)
        foreach (var path in new[] { "../victim.txt", @"..\victim.txt", @"sub\..\..\victim.txt",
                     "../uploads_secret/new/file.txt", "../UPLOADS/new/file.txt", "/../victim.txt",
                     "C:/victim.txt", "file.txt:stream" })
        {
            if (Path.DirectorySeparatorChar == '\\' && path == "../UPLOADS/new/file.txt")
                continue; // On Windows this resolves inside the root.
            yield return new TestCaseData(operation, path);
        }
    }

    private Task Mutate(string operation, string path)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("changed"));
        switch (operation)
        {
            case "write": files.WriteFile(path, "changed"); break;
            case "write-stream": files.WriteFile(path, stream); break;
            case "write-async": return files.WriteFileAsync(path, "changed");
            case "append": files.AppendFile(path, "changed"); break;
            case "append-stream": files.AppendFile(path, stream); break;
            case "delete": files.DeleteFile(path); break;
            case "delete-files": files.DeleteFiles(new[] { path }); break;
            case "delete-folder": files.DeleteFolder(path); break;
            default: throw new ArgumentException(operation);
        }
        return Task.CompletedTask;
    }

    [TestCaseSource(nameof(TraversalCases))]
    public void Mutations_reject_escape_before_touching_the_filesystem(string operation, string path)
    {
        Assert.ThrowsAsync<ArgumentException>(() => Mutate(operation, path));
        Assert.That(File.ReadAllText(Path.Combine(scratch, "victim.txt")), Is.EqualTo("original"));
        Assert.That(Directory.GetDirectories(scratch), Is.EquivalentTo(new[] { root }));
        Assert.That(Directory.GetFileSystemEntries(root), Is.Empty);
    }

    [Test]
    public async Task Normal_paths_leading_slashes_and_both_separators_work()
    {
        files.WriteFile("/nested/file.txt", "a");
        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("b")))
            files.AppendFile(@"nested\file.txt", stream);
        Assert.That(files.GetFile("/nested/file.txt").ReadAllText(), Is.EqualTo("ab"));
        await files.WriteFileAsync("nested/other.txt", new MemoryStream(Encoding.UTF8.GetBytes("async")));
        Assert.That(files.GetFile("nested/other.txt").ReadAllText(), Is.EqualTo("async"));
        Assert.That(files.GetDirectory("/"), Is.SameAs(files.RootDirectory));
        Assert.That(files.FileExists("nested/other.txt"), Is.True);
        Assert.That(files.DirectoryExists("nested"), Is.True);
        files.DeleteFile("/nested/file.txt");
        files.DeleteFolder("/nested");
        Assert.That(Directory.GetFileSystemEntries(root), Is.Empty);
    }

    [Test]
    public void Lexical_helper_handles_boundaries_case_and_backslashes()
    {
        Assert.That(FileSystemVirtualFiles.IsPathSafe(root, "../uploads_secret/file.txt"), Is.False);
        Assert.That(FileSystemVirtualFiles.IsPathSafe(root, @"..\victim.txt"), Is.False);
        Assert.That(FileSystemVirtualFiles.IsPathSafe(root, "../UPLOADS/file.txt"),
            Is.EqualTo(Path.DirectorySeparatorChar == '\\'));
        Assert.That(FileSystemVirtualFiles.IsPathSafe(root + Path.DirectorySeparatorChar, "../victim.txt"), Is.False);
        Assert.That(FileSystemVirtualFiles.IsPathSafe(root, "sub/../file.txt"), Is.True);
        Assert.That(FileSystemVirtualFiles.IsPathSafe(root, ""), Is.True);
        Assert.That(FileSystemVirtualFiles.IsPathSafe(root, "C:/file.txt"), Is.False);
        Assert.That(FileSystemVirtualFiles.IsPathSafe(root, null), Is.False);
        Assert.That(FileSystemVirtualFiles.IsPathSafe(null, "file.txt"), Is.False);
    }

    [Test]
    public void Lookup_and_root_deletion_reject_invalid_paths()
    {
        Assert.Throws<ArgumentException>(() => files.GetFile("../victim.txt"));
        Assert.Throws<ArgumentException>(() => files.GetDirectory("../"));
        Assert.Throws<ArgumentException>(() => files.FileExists("../victim.txt"));
        Assert.Throws<ArgumentException>(() => files.DirectoryExists("../"));
        Assert.Throws<ArgumentException>(() => files.DeleteFolder("/"));
        Assert.That(Directory.Exists(root), Is.True);
    }

#if NET6_0_OR_GREATER
    [TestCaseSource(nameof(Operations))]
    public void Mutations_reject_symlinks_including_missing_targets(string operation)
    {
        var outside = Path.Combine(scratch, "outside");
        Directory.CreateDirectory(outside);
        CreateDirectoryLink(Path.Combine(root, "link"), outside);
        Assert.ThrowsAsync<ArgumentException>(() => Mutate(operation, "link/new/file.txt"));
        Assert.That(Directory.GetFileSystemEntries(outside), Is.Empty);
        File.CreateSymbolicLink(Path.Combine(root, "file-link"), Path.Combine(scratch, "victim.txt"));
        Assert.ThrowsAsync<ArgumentException>(() => Mutate(operation, "file-link"));
        Assert.That(File.ReadAllText(Path.Combine(scratch, "victim.txt")), Is.EqualTo("original"));
        File.CreateSymbolicLink(Path.Combine(root, "dangling"), Path.Combine(outside, "missing.txt"));
        Assert.ThrowsAsync<ArgumentException>(() => Mutate(operation, "dangling"));
    }

    [Test]
    public void Recursive_delete_rejects_links_before_deleting_anything()
    {
        files.WriteFile("tree/keep.txt", "keep");
        CreateDirectoryLink(Path.Combine(root, "tree/link"), scratch);
        Assert.Throws<ArgumentException>(() => files.DeleteFolder("tree"));
        Assert.That(File.ReadAllText(Path.Combine(root, "tree/keep.txt")), Is.EqualTo("keep"));
        Directory.Delete(Path.Combine(root, "tree/link"));
        File.CreateSymbolicLink(Path.Combine(root, "tree/file-link"), Path.Combine(scratch, "victim.txt"));
        Assert.Throws<ArgumentException>(() => files.DeleteFolder("tree"));
        Assert.That(File.ReadAllText(Path.Combine(root, "tree/keep.txt")), Is.EqualTo("keep"));
    }

    [Test]
    public void Trusted_root_can_be_a_symlink_but_lookup_below_it_rejects_links()
    {
        var rootLink = Path.Combine(scratch, "root-link");
        CreateDirectoryLink(rootLink, root);
        var linked = new FileSystemVirtualFiles(rootLink);
        linked.WriteFile("safe/file.txt", "safe");
        Assert.That(File.ReadAllText(Path.Combine(root, "safe/file.txt")), Is.EqualTo("safe"));
        CreateDirectoryLink(Path.Combine(root, "escape"), scratch);
        Assert.Throws<ArgumentException>(() => linked.GetFile("escape/victim.txt"));
        Assert.Throws<ArgumentException>(() => linked.GetDirectory("escape"));
    }

    private static void CreateDirectoryLink(string path, string target)
    {
        try { Directory.CreateSymbolicLink(path, target); }
        catch (UnauthorizedAccessException) { Assert.Ignore("Creating symlinks requires permission on this host."); }
        catch (PlatformNotSupportedException) { Assert.Ignore("Symlinks are not supported on this host."); }
    }
#endif
}

[TestFixture]
public class FilesUploadPathSecurityTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase(".")]
    [TestCase("..")]
    [TestCase("../outside.txt")]
    [TestCase(@"..\outside.txt")]
    [TestCase(@"..\../outside.txt")]
    [TestCase("/tmp/outside.txt")]
    [TestCase("C:outside.txt")]
    [TestCase("file.txt:stream")]
    [TestCase("bad\0.txt")]
    public void Filename_is_rejected_before_custom_resolver_is_called(string name)
    {
        var called = false;
        var location = new UploadLocation("files", new MemoryVirtualFiles(), resolvePath: _ => {
            called = true;
            return "/uploads/files/safe.txt";
        });
        var feature = new FilesUploadFeature(location);
        var file = new HttpFile { FileName = name, Name = "file" };
        var error = Assert.Throws<HttpError>(() => feature.ResolveUploadFilePath(
            new FilesUploadContext(feature, location, new BasicRequest(), file)));
        Assert.That(error.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(called, Is.False);
    }

    [TestCase("/uploads/files/../other/file.txt")]
    [TestCase("/uploads/files/./file.txt")]
    [TestCase(@"/uploads/files/..\file.txt")]
    [TestCase("//server/share/file.txt")]
    [TestCase("/uploads/files/C:/file.txt")]
    [TestCase("/uploads/files/.. /file.txt")]
    [TestCase("/uploads/files/bad\0.txt")]
    public void Custom_resolver_paths_are_validated_for_every_provider(string path)
    {
        var location = new UploadLocation("files", new MemoryVirtualFiles(), resolvePath: _ => path);
        var feature = new FilesUploadFeature(location);
        Assert.Throws<HttpError>(() => feature.ResolveUploadFilePath(new FilesUploadContext(feature, location,
            new BasicRequest(), new HttpFile { FileName = "safe.txt", Name = "file" })));
    }

    [TestCase("/uploads/files/nested/safe.txt", "/files/nested/safe.txt")]
    [TestCase("/uploads-other/safe.txt", "/uploads-other/safe.txt")]
    [TestCase("/uploads/files/%2e%2e/safe.txt", "/files/%2e%2e/safe.txt")]
    public void Valid_paths_are_preserved_without_decoding(string path, string expected)
    {
        var location = new UploadLocation("files", new MemoryVirtualFiles(), resolvePath: _ => path);
        var feature = new FilesUploadFeature(location);
        var result = feature.ResolveUploadFilePath(new FilesUploadContext(feature, location,
            new BasicRequest(), new HttpFile { FileName = "safe.txt", Name = "file" }));
        Assert.That(result.PublicPath, Is.EqualTo(path));
        Assert.That(result.VirtualPath, Is.EqualTo(expected));
    }

    [Test]
    public async Task Default_upload_works_and_transformed_filenames_are_validated()
    {
        var storage = new MemoryVirtualFiles();
        var location = new UploadLocation("files", storage, writeAccessRole: RoleNames.AllowAnon);
        var feature = new FilesUploadFeature(location);
        using var host = new BasicAppHost { ConfigureAppHost = app => app.Plugins.Add(feature) }.Init();
        var req = new BasicRequest();
        var file = new HttpFile { Name = "file", FileName = "safe.txt", InputStream = new MemoryStream(new byte[] { 1, 2, 3 }) };
        var result = await feature.UploadFileAsync(location, req, new AuthUserSession(), file);
        Assert.That(storage.GetFile(result.Substring(feature.BasePath.Length)).ReadAllBytes(), Is.EqualTo(new byte[] { 1, 2, 3 }));
        var transformed = false;
        location.TransformFileAsync = _ => {
            transformed = true;
            return Task.FromResult<IHttpFile>(file);
        };
        Assert.ThrowsAsync<HttpError>(() => feature.UploadFileAsync(location, req, new AuthUserSession(),
            new HttpFile { Name = "file", FileName = "../outside.txt" }));
        Assert.That(transformed, Is.False);
        location.TransformFileAsync = _ => Task.FromResult<IHttpFile>(new HttpFile { Name = "file", FileName = @"..\outside.txt" });
        Assert.ThrowsAsync<HttpError>(() => feature.UploadFileAsync(location, req, new AuthUserSession(), file));
        Assert.That(storage.GetAllFiles().Count(), Is.EqualTo(1));
    }

    [TestCase("files/../outside.txt")]
    [TestCase(@"files\..\outside.txt")]
    public void Read_replace_and_delete_validate_paths_before_provider_access(string path)
    {
        var location = new UploadLocation("files", new MemoryVirtualFiles(), readAccessRole: RoleNames.AllowAnon,
            writeAccessRole: RoleNames.AllowAnon);
        var feature = new FilesUploadFeature(location);
        var req = new BasicRequest { Files = new IHttpFile[] { new HttpFile { FileName = "safe.txt", Name = "file" } } };
        var session = new AuthUserSession();
        Assert.ThrowsAsync<HttpError>(() => feature.GetFileAsync(location, req, session, path));
        Assert.ThrowsAsync<HttpError>(() => feature.ReplaceFileAsync(location, req, session, path));
        Assert.ThrowsAsync<HttpError>(() => feature.DeleteFileAsync(location, req, session, path));
    }
}
