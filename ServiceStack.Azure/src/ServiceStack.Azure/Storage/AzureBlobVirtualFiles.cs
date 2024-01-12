using ServiceStack.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using ServiceStack.VirtualPath;

namespace ServiceStack.Azure.Storage;

public class AzureBlobVirtualFiles : AbstractVirtualPathProviderBase, IVirtualFiles
{
    public CloudBlobContainer Container { get; }

    private readonly AzureBlobVirtualDirectory rootDirectory;

    public override IVirtualDirectory RootDirectory => rootDirectory;

    public override string VirtualPathSeparator => "/";

    public override string RealPathSeparator => "/";

    public AzureBlobVirtualFiles(string connectionString, string containerName)
    {
        var storageAccount = CloudStorageAccount.Parse(connectionString);

        //containerName  is the name of Azure Storage Blob container
        Container = storageAccount.CreateCloudBlobClient().GetContainerReference(containerName);
        Container.CreateIfNotExists();
        rootDirectory = new AzureBlobVirtualDirectory(this, null);
    }

    public AzureBlobVirtualFiles(CloudBlobContainer container)
    {
        Container = container;
        Container.CreateIfNotExists();
        rootDirectory = new AzureBlobVirtualDirectory(this, null);
    }

    protected override void Initialize()
    {
    }

    public void WriteFile(string filePath, string textContents)
    {
        var blob = Container.GetBlockBlobReference(SanitizePath(filePath));
        blob.Properties.ContentType = MimeTypes.GetMimeType(filePath);
        blob.UploadText(textContents);
    }

    public void WriteFile(string filePath, Stream stream)
    {
        var blob = Container.GetBlockBlobReference(SanitizePath(filePath));

        if (stream.Length > 1014 * 1024 * 100) // 100 mb
        {
            blob.ServiceClient.DefaultRequestOptions.SingleBlobUploadThresholdInBytes = 1014 * 1024 * 10;
            blob.ServiceClient.DefaultRequestOptions.ParallelOperationThreadCount = Environment.ProcessorCount;
        }

        blob.Properties.ContentType = MimeTypes.GetMimeType(filePath);
        blob.UploadFromStream(stream);
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
        var blob = Container.GetBlockBlobReference(SanitizePath(filePath));
        blob.Delete();
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
        // Delete based on a wildcard search of the directory
        if (!dirPath.EndsWith("/")) dirPath += "/";
        //directoryPath += "*";
        foreach (var blob in Container.ListBlobs(dirPath, true))
        {
            Container.GetBlockBlobReference(((CloudBlockBlob)blob).Name).DeleteIfExists();
        }
    }

    public override IVirtualFile? GetFile(string virtualPath)
    {
        var filePath = SanitizePath(virtualPath);

        var blob = Container.GetBlockBlobReference(filePath);
        if (!blob.Exists())
            return null;

        return new AzureBlobVirtualFile(this, GetDirectory(GetDirPath(virtualPath))).Init(blob);
    }

    public override IVirtualDirectory GetDirectory(string? virtualPath)
    {
        return new AzureBlobVirtualDirectory(this, virtualPath);
    }

    public override bool DirectoryExists(string virtualPath)
    {
        var ret = ((AzureBlobVirtualDirectory)GetDirectory(virtualPath)).Exists();
        return ret;
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

    public string? GetDirPath(CloudBlockBlob blob) => GetDirPath(blob.Parent?.Prefix);

    public IEnumerable<AzureBlobVirtualFile> EnumerateFiles(string? dirPath = null)
    {
        return Container.ListBlobs(dirPath == null ? null : dirPath + this.RealPathSeparator, useFlatBlobListing: true)
            .OfType<CloudBlockBlob>()
            .Select(q => new AzureBlobVirtualFile(this, new AzureBlobVirtualDirectory(this, GetDirPath(q))).Init(q));
    }

    public IEnumerable<AzureBlobVirtualFile> GetImmediateFiles(string? fromDirPath)
    {
        var dir = new AzureBlobVirtualDirectory(this, fromDirPath);

        return Container.ListBlobs(fromDirPath == null ? null : fromDirPath + this.RealPathSeparator)
            .OfType<CloudBlockBlob>()
            .Select(q => new AzureBlobVirtualFile(this, dir).Init(q as CloudBlockBlob));
    }

    public override string? SanitizePath(string filePath)
    {
        var sanitizedPath = string.IsNullOrEmpty(filePath)
            ? null
            : (filePath[0] == VirtualPathSeparator[0] ? filePath.Substring(1) : filePath);

        return sanitizedPath?.Replace('\\', VirtualPathSeparator[0]);
    }
}