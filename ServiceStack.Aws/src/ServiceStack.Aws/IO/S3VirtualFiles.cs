using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using ServiceStack.Aws;
using ServiceStack.Aws.S3;
using ServiceStack.Text;
using ServiceStack.VirtualPath;

namespace ServiceStack.IO;

public partial class S3VirtualFiles : AbstractVirtualPathProviderBase, IVirtualFiles
{
    public const int MultiObjectLimit = 1000;

    public IAmazonS3 AmazonS3 { get; private set; }
    public string BucketName { get; private set; }
    protected readonly S3VirtualDirectory rootDirectory;

    public S3VirtualFiles(IAmazonS3 client, string bucketName)
    {
        this.AmazonS3 = client;
        this.BucketName = bucketName;
        this.rootDirectory = new S3VirtualDirectory(this, null, null);
    }

    public const char DirSep = '/';

    public override IVirtualDirectory RootDirectory => rootDirectory;

    public override string VirtualPathSeparator => "/";

    public override string RealPathSeparator => "/";

    protected override void Initialize() {}

    public override IVirtualFile GetFile(string virtualPath)
    {
        if (string.IsNullOrEmpty(virtualPath))
            return null;

        var filePath = SanitizePath(virtualPath);
        try
        {
            var response = AmazonS3.GetObject(new GetObjectRequest
            {
                Key = filePath,
                BucketName = BucketName,
            });

            var dirPath = GetDirPath(filePath);
            var dir = dirPath == null
                ? RootDirectory
                : GetParentDirectory(dirPath);
            return new S3VirtualFile(this, dir).Init(response);
        }
        catch (AmazonS3Exception ex)
        {
            if (ex.StatusCode == HttpStatusCode.NotFound)
                return null;

            throw;
        }
    }

    protected S3VirtualDirectory GetParentDirectory(string dirPath)
    {
        if (string.IsNullOrEmpty(dirPath))
            return null;
        
        var parentDirPath = GetDirPath(dirPath.TrimEnd(DirSep));
        var parentDir = parentDirPath != null
            ? GetParentDirectory(parentDirPath)
            : (S3VirtualDirectory)RootDirectory;
        return new S3VirtualDirectory(this, dirPath, parentDir);
    }

    public override IVirtualDirectory GetDirectory(string virtualPath)
    {
        if (virtualPath == null)
            return null;

        var dirPath = SanitizePath(virtualPath);
        if (string.IsNullOrEmpty(dirPath))
            return RootDirectory;

        var seekPath = dirPath[dirPath.Length - 1] != DirSep
            ? dirPath + DirSep
            : dirPath;

        var response = AmazonS3.ListObjects(new ListObjectsRequest
        {
            BucketName = BucketName,
            Prefix = seekPath,
            MaxKeys = 1,
        });

        if (response.S3Objects.Count == 0)
            return null;

        return new S3VirtualDirectory(this, dirPath, GetParentDirectory(dirPath));
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
        AmazonS3.PutObject(new PutObjectRequest
        {
            Key = SanitizePath(filePath),
            BucketName = BucketName,
            ContentBody = contents,
        });
    }

    public virtual void WriteFile(string filePath, Stream stream)
    {
        AmazonS3.PutObject(new PutObjectRequest
        {
            Key = SanitizePath(filePath),
            BucketName = BucketName,
            InputStream = stream,
        });
    }

    public override async Task WriteFileAsync(string filePath, object contents, CancellationToken token = default)
    {
        // need to buffer otherwise hangs when trying to send an uploaded file stream (depends on provider)
        var buffer = contents is not MemoryStream;
        var fileContents = await FileContents.GetAsync(contents, buffer);
        if (fileContents?.Stream != null)
        {
            await AmazonS3.PutObjectAsync(new PutObjectRequest
            {
                Key = SanitizePath(filePath),
                BucketName = BucketName,
                InputStream = fileContents.Stream,
            }, token).ConfigAwait();
        }
        else if (fileContents?.Text != null)
        {
            await AmazonS3.PutObjectAsync(new PutObjectRequest
            {
                Key = SanitizePath(filePath),
                BucketName = BucketName,
                ContentBody = fileContents.Text,
            }, token).ConfigAwait();
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
        throw new NotImplementedException("S3 doesn't support appending to files");
    }

    public virtual void AppendFile(string filePath, Stream stream)
    {
        throw new NotImplementedException("S3 doesn't support appending to files");
    }

    public virtual void DeleteFile(string filePath)
    {
        AmazonS3.DeleteObject(new DeleteObjectRequest {
            BucketName = BucketName,
            Key = SanitizePath(filePath),
        });
    }

    public virtual void DeleteFiles(IEnumerable<string> filePaths)
    {
        var batches = filePaths
            .BatchesOf(MultiObjectLimit);

        foreach (var batch in batches)
        {
            var request = new DeleteObjectsRequest {
                BucketName = BucketName,
            };

            foreach (var filePath in batch)
            {
                request.AddKey(SanitizePath(filePath));
            }

            AmazonS3.DeleteObjects(request);
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

    public virtual IEnumerable<S3VirtualFile> EnumerateFiles(string prefix = null)
    {
        var response = AmazonS3.ListObjects(new ListObjectsRequest
        {
            BucketName = BucketName,
            Prefix = prefix,
        });

        foreach (var file in response.S3Objects)
        {
            var filePath = SanitizePath(file.Key);

            var dirPath = GetDirPath(filePath);
            yield return new S3VirtualFile(this, new S3VirtualDirectory(this, dirPath, GetParentDirectory(dirPath)))
            {
                FilePath = filePath,
                ContentLength = file.Size,
                FileLastModified = file.LastModified,
                Etag = file.ETag,
            };
        }
    }

#if NET6_0_OR_GREATER
    public virtual async IAsyncEnumerable<S3VirtualFile> EnumerateFilesAsync(string prefix = null, CancellationToken token = default)
    {
        ListObjectsV2Response response = null;

        while (true)
        {
            response = await AmazonS3.ListObjectsV2Async(new ListObjectsV2Request {
                BucketName = BucketName,
                Prefix = prefix,
                ContinuationToken = response?.NextContinuationToken
            }, token);

            foreach (var file in response.S3Objects)
            {
                var filePath = SanitizePath(file.Key);

                var dirPath = GetDirPath(filePath);
                yield return new S3VirtualFile(this, new S3VirtualDirectory(this, dirPath, GetParentDirectory(dirPath)))
                {
                    FilePath = filePath,
                    ContentLength = file.Size,
                    FileLastModified = file.LastModified,
                    Etag = file.ETag,
                };
            }

            if (!response.IsTruncated)
                yield break;
        }
    }
#endif

    public override IEnumerable<IVirtualFile> GetAllFiles()
    {
        return EnumerateFiles();
    }

#if NET6_0_OR_GREATER
    public IAsyncEnumerable<S3VirtualFile> GetAllFilesAsync(CancellationToken token=default) => EnumerateFilesAsync(token:token);
#endif

    public virtual IEnumerable<S3VirtualDirectory> GetImmediateDirectories(string fromDirPath)
    {
        var files = EnumerateFiles(fromDirPath);
        var dirPaths = files
            .Map(x => x.DirPath)
            .Distinct()
            .Map(x => GetImmediateSubDirPath(fromDirPath, x))
            .Where(x => x != null)
            .Distinct();

        return dirPaths.Select(x => new S3VirtualDirectory(this, x, GetParentDirectory(x)));
    }

#if NET6_0_OR_GREATER
    public virtual IAsyncEnumerable<S3VirtualDirectory> GetImmediateDirectoriesAsync(string fromDirPath, CancellationToken token=default)
    {
        var dirPaths = EnumerateFilesAsync(fromDirPath, token)
            .Select(x => x.DirPath)
            .Distinct()
            .Select(x => GetImmediateSubDirPath(fromDirPath, x))
            .Where(x => x != null)
            .Distinct();

        var parentDir = GetParentDirectory(fromDirPath);
        return dirPaths.Select(x => new S3VirtualDirectory(this, x, parentDir));
    }
#endif
    
    public virtual IEnumerable<S3VirtualFile> GetImmediateFiles(string fromDirPath)
    {
        return EnumerateFiles(fromDirPath)
            .Where(x => x.DirPath == fromDirPath);
    }
    
#if NET6_0_OR_GREATER
    public virtual IAsyncEnumerable<S3VirtualFile> GetImmediateFilesAsync(string fromDirPath, CancellationToken token=default)
    {
        return EnumerateFilesAsync(fromDirPath, token)
            .Where(x => x.DirPath == fromDirPath);
    }
#endif

    public virtual string GetDirPath(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return null;

        var lastDirPos = filePath.LastIndexOf(DirSep);
        return lastDirPos >= 0
            ? filePath.Substring(0, lastDirPos)
            : null;
    }

    public virtual string GetImmediateSubDirPath(string fromDirPath, string subDirPath)
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

    public override string SanitizePath(string filePath)
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
}

public partial class S3VirtualFiles : IS3Client
{
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