using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.IO;
using ServiceStack.Testing;
using ServiceStack.Web;

namespace ServiceStack.Extensions.Tests;

public enum UploadStorageProvider
{
    Memory,
    FileSystem,
}

[TestFixture(UploadStorageProvider.Memory)]
[TestFixture(UploadStorageProvider.FileSystem)]
public class FilesUploadCompatibilityTests
{
    private readonly UploadStorageProvider storageProvider;
    private readonly List<Stream> streams = new();
    private string scratch;
    private IVirtualFiles storage;
    private UploadLocation location;
    private FilesUploadFeature feature;
    private ServiceStackHost host;
    private AuthUserSession session;

    public FilesUploadCompatibilityTests(UploadStorageProvider storageProvider) => this.storageProvider = storageProvider;

    [SetUp]
    public void SetUp()
    {
        scratch = Path.Combine(Path.GetTempPath(), "ss-upload-compatibility-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(scratch);
        storage = storageProvider switch {
            UploadStorageProvider.Memory => new MemoryVirtualFiles(),
            UploadStorageProvider.FileSystem => new FileSystemVirtualFiles(scratch),
            _ => throw new ArgumentOutOfRangeException(nameof(storageProvider)),
        };
        location = new UploadLocation("files", storage);
        feature = new FilesUploadFeature(location);
        host = new BasicAppHost {
            ConfigureAppHost = app => {
                app.Plugins.Add(new AuthFeature(() => new AuthUserSession(), new IAuthProvider[] { new CredentialsAuthProvider() }));
                app.Plugins.Add(feature);
            }
        }.Init();
        session = new AuthUserSession {
            Id = "upload-session", UserAuthId = "42", UserAuthName = "upload-user",
            IsAuthenticated = true, Roles = new List<string>(), Permissions = new List<string>()
        };
    }

    [TearDown]
    public void TearDown()
    {
        host?.Dispose();
        foreach (var stream in streams)
            stream.Dispose();
        streams.Clear();
        Directory.Delete(scratch, true);
    }

    private HttpFile File(string name, string content)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        streams.Add(stream);
        return new HttpFile {
            Name = "file", FileName = name, ContentType = MimeTypes.PlainText,
            ContentLength = stream.Length, InputStream = stream
        };
    }

    private BasicRequest Request(params IHttpFile[] files)
    {
        var req = new BasicRequest { Files = files };
        req.Items[Keywords.Session] = session;
        return req;
    }

    private Task<string> Upload(string name, string content)
    {
        var file = File(name, content);
        return feature.UploadFileAsync(location, Request(file), session, file);
    }

    private string VirtualPath(string publicPath) => publicPath.Substring(feature.BasePath.Length);

    [TestCase("safe.txt")]
    [TestCase("annual report 2026.txt")]
    [TestCase("résumé-日本語.txt")]
    [TestCase("archive.backup.tar.gz")]
    [TestCase(".profile")]
    [TestCase("100% complete.txt")]
    [TestCase("%2e%2e.txt")]
    public async Task Roleless_authenticated_user_can_upload_download_replace_and_delete(string name)
    {
        Assert.That(location.ReadAccessRole, Is.EqualTo(RoleNames.AllowAnyUser));
        Assert.That(location.WriteAccessRole, Is.EqualTo(RoleNames.AllowAnyUser));
        Assert.That(session.Roles, Is.Empty);
        var publicPath = await Upload(name, "original ✓");
        Assert.That(publicPath, Does.StartWith("/uploads/files/").And.EndWith("/" + name));
        var path = VirtualPath(publicPath);
        var downloaded = await feature.GetFileAsync(location, Request(), session, path);
        Assert.That(downloaded.Name, Is.EqualTo(name));
        Assert.That(downloaded.ReadAllBytes(), Is.EqualTo(Encoding.UTF8.GetBytes("original ✓")));
        await feature.ReplaceFileAsync(location, Request(File(name, "replaced ✓")), session, path);
        Assert.That(storage.GetFile(path).ReadAllText(), Is.EqualTo("replaced ✓"));
        Assert.That(await feature.DeleteFileAsync(location, Request(), session, path), Is.True);
        Assert.That(storage.GetFile(path), Is.Null);
        Assert.That(storage.GetAllFiles(), Is.Empty);
    }

    [Test]
    public async Task Create_only_uploads_create_new_files_and_preserve_existing_files()
    {
        location.AllowOperations = FilesUploadOperation.Create;
        location.ResolvePath = ctx => ctx.GetLocationPath("tenant-42/file.txt");
        var path = VirtualPath(await Upload("file.txt", "original"));
        var error = Assert.ThrowsAsync<HttpError>(() => Upload("file.txt", "overwrite"));
        Assert.That(error.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That(storage.GetFile(path).ReadAllText(), Is.EqualTo("original"));
        Assert.That(storage.GetAllFiles().Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task Update_only_uploads_replace_existing_files_and_reject_new_files()
    {
        storage.WriteFile("files/tenant-42/existing.txt", "original");
        location.AllowOperations = FilesUploadOperation.Update;
        location.ResolvePath = ctx => ctx.GetLocationPath("tenant-42/" + ctx.FileName);
        var path = VirtualPath(await Upload("existing.txt", "updated"));
        Assert.That(storage.GetFile(path).ReadAllText(), Is.EqualTo("updated"));
        var error = Assert.ThrowsAsync<HttpError>(() => Upload("new.txt", "new"));
        Assert.That(error.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That(storage.GetFile("files/tenant-42/new.txt"), Is.Null);
        await feature.ReplaceFileAsync(location, Request(File("existing.txt", "replaced")), session, path);
        Assert.That(storage.GetFile(path).ReadAllText(), Is.EqualTo("replaced"));
    }

    [TestCase(FilesUploadOperation.Read)]
    [TestCase(FilesUploadOperation.Update)]
    [TestCase(FilesUploadOperation.Delete)]
    public async Task Each_operation_remains_limited_to_its_permission(FilesUploadOperation allowed)
    {
        const string path = "files/permissions.txt";
        storage.WriteFile(path, "original");
        location.AllowOperations = allowed;
        if (allowed == FilesUploadOperation.Read)
            Assert.That((await feature.GetFileAsync(location, Request(), session, path)).ReadAllText(), Is.EqualTo("original"));
        else
            Assert.That(Assert.ThrowsAsync<HttpError>(() => feature.GetFileAsync(location, Request(), session, path)).StatusCode,
                Is.EqualTo(HttpStatusCode.NotFound));
        if (allowed == FilesUploadOperation.Update)
            await feature.ReplaceFileAsync(location, Request(File("permissions.txt", "updated")), session, path);
        else
            Assert.That(Assert.ThrowsAsync<HttpError>(() => feature.ReplaceFileAsync(location,
                Request(File("permissions.txt", "unexpected")), session, path)).StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That(storage.GetFile(path).ReadAllText(), Is.EqualTo(allowed == FilesUploadOperation.Update ? "updated" : "original"));
        if (allowed == FilesUploadOperation.Delete)
        {
            Assert.That(await feature.DeleteFileAsync(location, Request(), session, path), Is.True);
            Assert.That(storage.GetFile(path), Is.Null);
        }
        else
        {
            Assert.That(Assert.ThrowsAsync<HttpError>(() => feature.DeleteFileAsync(location, Request(), session, path)).StatusCode,
                Is.EqualTo(HttpStatusCode.Forbidden));
            Assert.That(storage.GetFile(path), Is.Not.Null);
        }
    }

    [Test]
    public void Default_access_rejects_unauthenticated_requests_without_modifying_files()
    {
        storage.WriteFile("files/private.txt", "private");
        session.IsAuthenticated = false;
        Assert.That(Assert.ThrowsAsync<HttpError>(() => Upload("new.txt", "unexpected")).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(Assert.ThrowsAsync<HttpError>(() => feature.GetFileAsync(location, Request(), session, "files/private.txt")).StatusCode,
            Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(Assert.ThrowsAsync<HttpError>(() => feature.ReplaceFileAsync(location, Request(File("private.txt", "unexpected")), session,
            "files/private.txt")).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(Assert.ThrowsAsync<HttpError>(() => feature.DeleteFileAsync(location, Request(), session, "files/private.txt")).StatusCode,
            Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(storage.GetFile("files/private.txt").ReadAllText(), Is.EqualTo("private"));
        Assert.That(storage.GetAllFiles().Count(), Is.EqualTo(1));
    }

    [TestCase("/uploads")]
    [TestCase("/assets/uploads")]
    public async Task Custom_resolver_and_transform_preserve_user_paths_urls_and_bytes(string basePath)
    {
        feature.BasePath = basePath;
        location.ResolvePath = ctx => ctx.GetLocationPath($"tenants/team-1/users/{ctx.UserAuthId}/{ctx.FileName}");
        location.TransformFileAsync = ctx => Task.FromResult<IHttpFile>(File("processed.txt", "transformed ✓"));
        var publicPath = await Upload("original.txt", "original");
        Assert.That(publicPath, Is.EqualTo(basePath + "/files/tenants/team-1/users/42/processed.txt"));
        var stored = storage.GetFile(VirtualPath(publicPath));
        Assert.That(stored.ReadAllBytes(), Is.EqualTo(Encoding.UTF8.GetBytes("transformed ✓")));
        Assert.That(storage.GetAllFiles().Single().VirtualPath, Is.EqualTo("files/tenants/team-1/users/42/processed.txt"));
    }

    [Test]
    public async Task Valid_transform_can_skip_an_upload_without_creating_files()
    {
        location.TransformFileAsync = _ => Task.FromResult<IHttpFile>(null);
        Assert.That(await Upload("skip.txt", "skip"), Is.Null);
        Assert.That(storage.GetAllFiles(), Is.Empty);
    }
}

[TestFixture]
public class FileSystemUploadCompatibilityTests
{
    private string scratch;
    private string root;

    [SetUp]
    public void SetUp()
    {
        scratch = Path.Combine(Path.GetTempPath(), "ss-vfs-compatibility-" + Guid.NewGuid().ToString("N"));
        root = Path.Combine(scratch, "uploads");
        Directory.CreateDirectory(root);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(scratch, true);

    public static IEnumerable<TestCaseData> MutationCases()
    {
        foreach (var operation in new[] { "write", "write-stream", "write-async", "append", "append-stream", "delete", "delete-files", "delete-folder" })
        foreach (var path in new[] { "nested/file.txt", "/nested/file.txt", @"nested\file.txt", @"\nested\file.txt" })
            yield return new TestCaseData(operation, path);
    }

    [TestCaseSource(nameof(MutationCases))]
    public async Task Every_mutation_supports_valid_nested_virtual_paths(string operation, string path)
    {
        var files = new FileSystemVirtualFiles(root);
        files.WriteFile("nested/file.txt", "original");
        files.WriteFile("keep.txt", "keep");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("changed"));
        switch (operation)
        {
            case "write": files.WriteFile(path, "changed"); break;
            case "write-stream": files.WriteFile(path, stream); break;
            case "write-async": await files.WriteFileAsync(path, stream); break;
            case "append": files.AppendFile(path, "changed"); break;
            case "append-stream": files.AppendFile(path, stream); break;
            case "delete": files.DeleteFile(path); break;
            case "delete-files":
                files.WriteFile("second.txt", "second");
                files.DeleteFiles(new[] { path, "/second.txt" });
                Assert.That(files.FileExists("second.txt"), Is.False);
                break;
            case "delete-folder":
                files.WriteFile("nested/deeper/child.txt", "child");
                files.DeleteFolder(path.Substring(0, path.LastIndexOfAny(new[] { '/', '\\' })));
                Assert.That(files.DirectoryExists("nested"), Is.False);
                break;
        }
        if (operation.StartsWith("delete", StringComparison.Ordinal))
            Assert.That(files.FileExists("nested/file.txt"), Is.False);
        else
            Assert.That(files.GetFile("nested/file.txt").ReadAllText(),
                Is.EqualTo(operation.StartsWith("append", StringComparison.Ordinal) ? "originalchanged" : "changed"));
        Assert.That(files.GetFile("keep.txt").ReadAllText(), Is.EqualTo("keep"));
    }

    [TestCase("normal")]
    [TestCase("trailing-separator")]
    [TestCase("symlink")]
    public async Task Root_configurations_support_create_lookup_append_and_delete(string configuration)
    {
        var configuredRoot = root;
        if (configuration == "trailing-separator")
            configuredRoot += Path.DirectorySeparatorChar;
        else if (configuration == "symlink")
        {
            configuredRoot = Path.Combine(scratch, "root-link");
            try { Directory.CreateSymbolicLink(configuredRoot, root); }
            catch (UnauthorizedAccessException) { Assert.Ignore("Creating symlinks requires permission on this host."); }
            catch (PlatformNotSupportedException) { Assert.Ignore("Symlinks are not supported on this host."); }
        }
        var files = new FileSystemVirtualFiles(configuredRoot);
        await files.WriteFileAsync("/nested/file.txt", "original");
        files.AppendFile(@"nested\file.txt", " appended");
        Assert.That(files.GetFile("nested/file.txt").ReadAllText(), Is.EqualTo("original appended"));
        Assert.That(files.GetDirectory("/nested").Files.Single().VirtualPath, Is.EqualTo("nested/file.txt"));
        Assert.That(files.RootDirectory.Directories.Single().Name, Is.EqualTo("nested"));
        Assert.That(files.GetDirectory("/"), Is.SameAs(files.RootDirectory));
        Assert.That(files.GetDirectory(""), Is.SameAs(files.RootDirectory));
        Assert.That(files.GetFile("missing.txt"), Is.Null);
        Assert.That(files.GetDirectory("missing"), Is.Null);
        files.DeleteFolder("/nested");
        Assert.That(Directory.GetFileSystemEntries(root), Is.Empty);
        Assert.That(Directory.Exists(root), Is.True);
    }

    [Test]
    public async Task Canonical_paths_inside_the_root_remain_valid_for_direct_vfs_callers()
    {
        var files = new FileSystemVirtualFiles(root);
        await files.WriteFileAsync("nested/../safe.txt", "safe");
        Assert.That(files.GetFile("./safe.txt").ReadAllText(), Is.EqualTo("safe"));
        Assert.That(files.GetFile("nested/../safe.txt").VirtualPath, Is.EqualTo("safe.txt"));
        Assert.That(Directory.GetDirectories(root), Is.Empty);
    }

    [Test, Platform("Win")]
    public async Task Windows_drive_root_supports_virtual_paths_without_changing_other_files()
    {
        var driveRoot = Path.GetPathRoot(root);
        var files = new FileSystemVirtualFiles(driveRoot);
        var relativePath = Path.GetRelativePath(driveRoot, Path.Combine(root, "drive-root.txt")).Replace('\\', '/');
        await files.WriteFileAsync("/" + relativePath, "drive-root");
        Assert.That(files.GetFile(relativePath).ReadAllText(), Is.EqualTo("drive-root"));
        files.DeleteFile(relativePath);
        Assert.That(System.IO.File.Exists(Path.Combine(root, "drive-root.txt")), Is.False);
    }

    [Test, Platform("Win")]
    public void Windows_lookup_and_mutations_preserve_case_insensitive_paths()
    {
        var files = new FileSystemVirtualFiles(root);
        files.WriteFile("MixedCase/File.txt", "original");
        files.AppendFile("mixedcase/FILE.TXT", " appended");
        Assert.That(files.GetFile("MIXEDCASE/file.txt").ReadAllText(), Is.EqualTo("original appended"));
        files.DeleteFile("mixedcase/FILE.txt");
        files.DeleteFolder("MIXEDCASE");
        Assert.That(Directory.GetFileSystemEntries(root), Is.Empty);
    }
}
