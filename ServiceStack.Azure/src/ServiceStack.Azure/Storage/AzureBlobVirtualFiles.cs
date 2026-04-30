using ServiceStack.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using ServiceStack.VirtualPath;
using System.Threading;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ServiceStack.Azure.Storage;

public class AzureBlobVirtualFiles : AbstractVirtualPathProviderBase, IVirtualFiles
{
    public BlobContainerClient Container { get; }

    private readonly AzureBlobVirtualDirectory rootDirectory;

    public override IVirtualDirectory RootDirectory => rootDirectory;

    public override string VirtualPathSeparator => "/";

    public override string RealPathSeparator => "/";

    public AzureBlobVirtualFiles(string connectionString, string containerName)
    {
        Container = new BlobContainerClient(connectionString, containerName);
        Container.CreateIfNotExists();
        rootDirectory = new AzureBlobVirtualDirectory(this, null);
    }

    public AzureBlobVirtualFiles(BlobContainerClient container)
    {
        Container = container;
        Container.CreateIfNotExists();
        rootDirectory = new AzureBlobVirtualDirectory(this, null);
    }

    protected override void Initialize() { }

    public void WriteFile(string filePath, string textContents)
    {
        var blobPath = SanitizePath(filePath);
        var blob = Container.GetBlobClient(blobPath);
        blob.Upload(BinaryData.FromString(textContents), new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = MimeTypes.GetMimeType(filePath) }
        });
    }

    public void WriteFile(string filePath, Stream stream)
    {
        var blobPath = SanitizePath(filePath);
        var blob = Container.GetBlobClient(blobPath);
        blob.Upload(stream, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = MimeTypes.GetMimeType(filePath) }
        });
    }

    public void WriteFiles(IEnumerable<IVirtualFile> files, Func<IVirtualFile, string> toPath = null)
    {
        this.CopyFrom(files, toPath);
    }

    public void AppendFile(string filePath, string textContents)
    {
        throw new NotSupportedException();
    }

    public void AppendFile(string filePath, Stream stream)
    {
        throw new NotSupportedException();
    }

    public void DeleteFile(string filePath)
    {
        Container.GetBlobClient(SanitizePath(filePath)).Delete();
    }

    public void DeleteFiles(IEnumerable<string> filePaths)
    {
        filePaths.Each(DeleteFile);
    }

    public void DeleteFolder(string dirPath)
    {
        if (string.IsNullOrEmpty(dirPath))
            throw new ArgumentNullException(nameof(dirPath));

        dirPath = SanitizePath(dirPath)!;
        if (!dirPath.EndsWith("/")) dirPath = $"{dirPath}{RealPathSeparator}";

        foreach (var item in Container.GetBlobs(BlobTraits.None, BlobStates.None, dirPath, CancellationToken.None))
        {
            Container.GetBlobClient(item.Name).DeleteIfExists();
        }
    }

    public override IVirtualFile? GetFile(string virtualPath)
    {
        var filePath = SanitizePath(virtualPath);
        var blob = Container.GetBlobClient(filePath);
        if (!blob.Exists().Value)
            return null;

        var props = blob.GetProperties().Value;
        return new AzureBlobVirtualFile(this, GetDirectory(GetDirPath(virtualPath))).Init(blob, props);
    }

    public override IVirtualDirectory GetDirectory(string? virtualPath)
    {
        return new AzureBlobVirtualDirectory(this, virtualPath);
    }

    public override bool DirectoryExists(string virtualPath)
    {
        return ((AzureBlobVirtualDirectory)GetDirectory(virtualPath)).Exists();
    }

    public string? GetDirPath(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return null;

        var lastDirPos = filePath.LastIndexOf(VirtualPathSeparator[0]);
        return lastDirPos >= 0
            ? filePath.Substring(0, lastDirPos)
            : null;
    }

    public string? GetDirPath(BlobItem blob) => GetDirPath(blob.Name);

    public IEnumerable<AzureBlobVirtualFile> EnumerateFiles(string? dirPath = null)
    {
        var prefix = dirPath == null ? null : dirPath + RealPathSeparator;
        return Container.GetBlobs(BlobTraits.None, BlobStates.None, prefix, CancellationToken.None)
            .Select(item =>
            {
                var blobClient = Container.GetBlobClient(item.Name);
                var props = ToBlobProperties(item);
                return new AzureBlobVirtualFile(this, new AzureBlobVirtualDirectory(this, GetDirPath(item))).Init(blobClient, props);
            });
    }

    public IEnumerable<AzureBlobVirtualFile> GetImmediateFiles(string? fromDirPath)
    {
        var prefix = fromDirPath == null ? null : $"{fromDirPath}{RealPathSeparator}";
        var dir = new AzureBlobVirtualDirectory(this, fromDirPath);

        return Container.GetBlobsByHierarchy(BlobTraits.None, BlobStates.None, RealPathSeparator, prefix, CancellationToken.None)
            .Where(static x => x.IsBlob)
            .Select(x =>
            {
                var blobClient = Container.GetBlobClient(x.Blob.Name);
                var props = ToBlobProperties(x.Blob);
                return new AzureBlobVirtualFile(this, dir).Init(blobClient, props);
            });
    }

    public override string? SanitizePath(string filePath)
    {
        var sanitizedPath = string.IsNullOrEmpty(filePath)
            ? null
            : (filePath[0] == VirtualPathSeparator[0] ? filePath.Substring(1) : filePath);

        return sanitizedPath?.Replace('\\', VirtualPathSeparator[0]);
    }

    internal static BlobProperties ToBlobProperties(BlobItem item) =>
        BlobsModelFactory.BlobProperties(
            lastModified: item.Properties.LastModified ?? DateTimeOffset.MinValue,
            contentLength: item.Properties.ContentLength ?? 0,
            contentType: item.Properties.ContentType);
}
