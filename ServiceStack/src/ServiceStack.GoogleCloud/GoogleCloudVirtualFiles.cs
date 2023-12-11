using System.Net;
using Google;
using Google.Api.Gax;
using Google.Apis.Storage.v1.Data;
using Google.Cloud.Storage.V1;
using ServiceStack.IO;
using ServiceStack.Text;
using ServiceStack.VirtualPath;
using Object = Google.Apis.Storage.v1.Data.Object;

namespace ServiceStack.GoogleCloud;

public partial class GoogleCloudVirtualFiles : AbstractVirtualPathProviderBase, IVirtualFiles
{
    public const int MultiObjectLimit = 1000;

    public StorageClient StorageClient { get; private set; }
    public string BucketName { get; private set; }
    protected readonly GoogleCloudVirtualDirectory rootDirectory;

    public GoogleCloudVirtualFiles(StorageClient client, string bucketName)
    {
        this.StorageClient = client;
        this.BucketName = bucketName;
        this.rootDirectory = new GoogleCloudVirtualDirectory(this, null, null);
    }

    public const char DirSep = '/';

    public override IVirtualDirectory RootDirectory => rootDirectory;

    public override string VirtualPathSeparator => "/";

    public override string RealPathSeparator => "/";

    protected override void Initialize() {}

    public override IVirtualFile? GetFile(string virtualPath)
    {
        if (string.IsNullOrEmpty(virtualPath))
            return null;

        var filePath = SanitizePath(virtualPath);
        try
        {
            var response = StorageClient.GetObject(bucket:BucketName, objectName:filePath);

            var dirPath = GetDirPath(filePath);
            var dir = dirPath == null
                ? RootDirectory
                : GetParentDirectory(dirPath);
            return new GoogleCloudVirtualFile(this, dir).Init(response);
        }
        catch (GoogleApiException ex)
        {
            if (ex.HttpStatusCode == HttpStatusCode.NotFound)
                return null;

            throw;
        }
    }

    protected GoogleCloudVirtualDirectory? GetParentDirectory(string dirPath)
    {
        if (string.IsNullOrEmpty(dirPath))
            return null;

        var parentDirPath = GetDirPath(dirPath.TrimEnd(DirSep));
        var parentDir = parentDirPath != null
            ? GetParentDirectory(parentDirPath)
            : (GoogleCloudVirtualDirectory)RootDirectory;
        return new GoogleCloudVirtualDirectory(this, dirPath, parentDir);
    }

    public override IVirtualDirectory? GetDirectory(string? virtualPath)
    {
        if (virtualPath == null)
            return null;

        var dirPath = SanitizePath(virtualPath);
        if (string.IsNullOrEmpty(dirPath))
            return RootDirectory;

        var seekPath = dirPath[dirPath.Length - 1] != DirSep
            ? dirPath + DirSep
            : dirPath;

        PagedEnumerable<Objects, Object>? response = StorageClient.ListObjects(bucket:BucketName, prefix:seekPath);

        if (!response.Any())
            return null;

        return new GoogleCloudVirtualDirectory(this, dirPath, GetParentDirectory(dirPath));
    }

    public override bool DirectoryExists(string virtualPath)
    {
        return GetDirectory(virtualPath) != null;
    }

    public override bool FileExists(string virtualPath)
    {
        return GetFile(virtualPath) != null;
    }

    public virtual void WriteFile(string filePath, string contents)
    {
        using var ms = MemoryStreamFactory.GetStream();
        MemoryProvider.Instance.WriteUtf8ToStream(contents, ms);
        WriteFile(filePath, ms);
    }

    public virtual void WriteFile(string filePath, Stream stream)
    {
        StorageClient.UploadObject(
            bucket: BucketName,
            objectName: SanitizePath(filePath),
            contentType: MimeTypes.GetMimeType(filePath),
            source:stream);
    }

    public override async Task WriteFileAsync(string filePath, object contents, CancellationToken token = default)
    {
        // need to buffer otherwise hangs when trying to send an uploaded file stream (depends on provider)
        var buffer = contents is not MemoryStream;
        var fileContents = await FileContents.GetAsync(contents, buffer);
        if (fileContents?.Stream != null)
        {
            await StorageClient.UploadObjectAsync(
                bucket: BucketName,
                objectName: SanitizePath(filePath),
                contentType: MimeTypes.GetMimeType(filePath),
                source:fileContents.Stream,
                null, 
                token).ConfigAwait();
        }
        else if (fileContents?.Text != null)
        {
            using var ms = MemoryStreamFactory.GetStream();
            MemoryProvider.Instance.WriteUtf8ToStream(fileContents.Text, ms);
            
            await StorageClient.UploadObjectAsync(
                bucket: BucketName,
                objectName: SanitizePath(filePath),
                contentType: MimeTypes.GetMimeType(filePath),
                source:ms,
                null, 
                token).ConfigAwait();
        }
        else throw new NotSupportedException($"Unknown File Content Type: {contents.GetType().Name}");

        if (buffer && fileContents.Stream != null) // Dispose MemoryStream buffer created by FileContents
            using (fileContents.Stream) {}
    }

    public virtual void WriteFiles(IEnumerable<IVirtualFile> files, Func<IVirtualFile, string> toPath = null)
    {
        this.CopyFrom(files, toPath);
    }

    public virtual void AppendFile(string filePath, string textContents)
    {
        throw new NotImplementedException("Google Cloud doesn't support appending to files");
    }

    public virtual void AppendFile(string filePath, Stream stream)
    {
        throw new NotImplementedException("Google Cloud doesn't support appending to files");
    }

    public virtual void DeleteFile(string filePath)
    {
        StorageClient.DeleteObject(
            bucket:BucketName,
            objectName:SanitizePath(filePath));
    }

    public virtual void DeleteFiles(IEnumerable<string> filePaths)
    {
        //TODO: optimize when it's ever possible https://cloud.google.com/storage/docs/deleting-objects
        var batches = filePaths
            .BatchesOf(MultiObjectLimit);
        
        foreach (var batch in batches)
        {
            foreach (var filePath in batch)
            {
                DeleteFile(filePath);
            }
        }
    }

    public virtual void DeleteFolder(string dirPath)
    {
        dirPath = SanitizePath(dirPath);
        var nestedFiles = EnumerateFiles(dirPath).Map(x => x.FilePath);
        DeleteFiles(nestedFiles);
    }

#if NET6_0_OR_GREATER
    public virtual async Task DeleteFolderAsync(string dirPath, CancellationToken token=default)
    {
        dirPath = SanitizePath(dirPath);
        var nestedFiles = await EnumerateFilesAsync(dirPath, token).Select(x => x.FilePath).ToListAsync(token);
        DeleteFiles(nestedFiles);
    }
#endif    

    public virtual IEnumerable<GoogleCloudVirtualFile> EnumerateFiles(string? prefix = null)
    {
        var response = StorageClient.ListObjects(bucket:BucketName, prefix:prefix);

        foreach (var file in response)
        {
            var filePath = SanitizePath(file.Name);

            var dirPath = GetDirPath(filePath);
            yield return new GoogleCloudVirtualFile(this, new GoogleCloudVirtualDirectory(this, dirPath, GetParentDirectory(dirPath)))
            {
                FilePath = filePath,
                ContentLength = (long)(file.Size ?? 0),
                FileLastModified = file.Updated ?? DateTime.UtcNow,
                Etag = file.ETag,
            };
        }
    }

#if NET6_0_OR_GREATER
    public virtual async IAsyncEnumerable<GoogleCloudVirtualFile> EnumerateFilesAsync(string? prefix = null, CancellationToken token = default)
    {
        var response = StorageClient.ListObjectsAsync(
            bucket:BucketName,
            prefix:prefix);

        await foreach (var file in response)
        {
            var filePath = SanitizePath(file.Name);

            var dirPath = GetDirPath(filePath);
            yield return new GoogleCloudVirtualFile(this, new GoogleCloudVirtualDirectory(this, dirPath, GetParentDirectory(dirPath)))
            {
                FilePath = filePath,
                ContentLength = (long)(file.Size ?? 0),
                FileLastModified = file.Updated ?? DateTime.UtcNow,
                Etag = file.ETag,
            };
        }
    }
#endif

    public override IEnumerable<IVirtualFile> GetAllFiles()
    {
        return EnumerateFiles();
    }

#if NET6_0_OR_GREATER
    public IAsyncEnumerable<GoogleCloudVirtualFile> GetAllFilesAsync(CancellationToken token=default) => EnumerateFilesAsync(token:token);
#endif

    public virtual IEnumerable<GoogleCloudVirtualDirectory> GetImmediateDirectories(string fromDirPath)
    {
        var files = EnumerateFiles(fromDirPath);
        var dirPaths = files
            .Map(x => x.DirPath)
            .Distinct()
            .Map(x => GetImmediateSubDirPath(fromDirPath, x))
            .Where(x => x != null)
            .Distinct();

        return dirPaths.Select(x => new GoogleCloudVirtualDirectory(this, x, GetParentDirectory(x)));
    }

#if NET6_0_OR_GREATER
    public virtual IAsyncEnumerable<GoogleCloudVirtualDirectory> GetImmediateDirectoriesAsync(string fromDirPath, CancellationToken token=default)
    {
        var dirPaths = EnumerateFilesAsync(fromDirPath, token)
            .Select(x => x.DirPath)
            .Distinct()
            .Select(x => GetImmediateSubDirPath(fromDirPath, x))
            .Where(x => x != null)
            .Distinct();

        var parentDir = GetParentDirectory(fromDirPath);
        return dirPaths.Select(x => new GoogleCloudVirtualDirectory(this, x, parentDir));
    }
#endif
    
    public virtual IEnumerable<GoogleCloudVirtualFile> GetImmediateFiles(string fromDirPath)
    {
        return EnumerateFiles(fromDirPath)
            .Where(x => x.DirPath == fromDirPath);
    }
    
#if NET6_0_OR_GREATER
    public virtual IAsyncEnumerable<GoogleCloudVirtualFile> GetImmediateFilesAsync(string fromDirPath, CancellationToken token=default)
    {
        return EnumerateFilesAsync(fromDirPath, token)
            .Where(x => x.DirPath == fromDirPath);
    }
#endif

    public virtual string? GetDirPath(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return null;

        var lastDirPos = filePath.LastIndexOf(DirSep);
        return lastDirPos >= 0
            ? filePath.Substring(0, lastDirPos)
            : null;
    }

    public virtual string? GetImmediateSubDirPath(string? fromDirPath, string subDirPath)
    {
        if (string.IsNullOrEmpty(subDirPath))
            return null;

        if (fromDirPath == null)
        {
            return subDirPath.CountOccurrencesOf(DirSep) == 0 
                ? subDirPath
                : subDirPath.LeftPart(DirSep);
        }

        if (!subDirPath.StartsWith(fromDirPath))
            return null;

        return fromDirPath.CountOccurrencesOf(DirSep) == subDirPath.CountOccurrencesOf(DirSep) - 1 
            ? subDirPath
            : null;
    }

    public override string? SanitizePath(string filePath)
    {
        var sanitizedPath = string.IsNullOrEmpty(filePath)
            ? null
            : (filePath[0] == DirSep ? filePath.Substring(1) : filePath);

        return sanitizedPath?.Replace('\\', DirSep);
    }

    public static string GetFileName(string filePath)
    {
        return filePath.SplitOnLast(DirSep).Last();
    }
    
    public virtual void ClearBucket()
    {
        var allFilePaths = EnumerateFiles()
            .Map(x => x.FilePath);

        DeleteFiles(allFilePaths);
    }
 
#if NET6_0_OR_GREATER
    public virtual async Task ClearBucketAsync(CancellationToken token=default)
    {
        var allFilePaths = await EnumerateFilesAsync(token:token)
            .Select(x => x.FilePath).ToListAsync(token);

        DeleteFiles(allFilePaths);
    }
#endif
}