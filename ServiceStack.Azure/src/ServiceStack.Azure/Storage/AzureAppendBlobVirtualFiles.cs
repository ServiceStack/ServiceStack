using ServiceStack.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using ServiceStack.VirtualPath;
using Microsoft.WindowsAzure.Storage.RetryPolicies;

namespace ServiceStack.Azure.Storage;

public class AzureAppendBlobVirtualFiles : AbstractVirtualPathProviderBase, IVirtualFiles
{
    public CloudBlobContainer Container { get; }

    private readonly AzureAppendBlobVirtualDirectory rootDirectory;

    public override IVirtualDirectory RootDirectory => rootDirectory;

    public override string VirtualPathSeparator => "/";

    public override string RealPathSeparator => "/";

    public AzureAppendBlobVirtualFiles(string connectionString, string containerName)
    {
        var storageAccount = CloudStorageAccount.Parse(connectionString);

        //containerName  is the name of Azure Storage Blob container
        Container = storageAccount.CreateCloudBlobClient().GetContainerReference(containerName);
        Container.CreateIfNotExists();
        rootDirectory = new AzureAppendBlobVirtualDirectory(this, null);
    }

    public AzureAppendBlobVirtualFiles(CloudBlobContainer container)
    {
        Container = container;
        Container.CreateIfNotExists();
        rootDirectory = new AzureAppendBlobVirtualDirectory(this, null);
    }

    protected override void Initialize()
    {
    }

    public void WriteFile(string filePath, string textContents)
    {
        var blob = Container.GetAppendBlobReference(SanitizePath(filePath));
        blob.CreateOrReplace(null,null,null);
        blob.Properties.ContentType = MimeTypes.GetMimeType(filePath);
        blob.AppendText(textContents);
    }

    public void WriteFile(string filePath, Stream stream)
    {
        var blob = Container.GetAppendBlobReference(SanitizePath(filePath));
        blob.CreateOrReplace(AccessCondition.GenerateEmptyCondition(), new BlobRequestOptions() { RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(1), 10) }, null);
        blob.Properties.ContentType = MimeTypes.GetMimeType(filePath);
        blob.AppendFromStream(stream);
    }

    public void WriteFiles(IEnumerable<IVirtualFile> files, Func<IVirtualFile, string>? toPath = null)
    {
        this.CopyFrom(files, toPath);
    }

    public void AppendFile(string filePath, string textContents)
    {
        var blob = Container.GetAppendBlobReference(SanitizePath(filePath));
        blob.AppendText(textContents);

    }

    public void AppendFile(string filePath, Stream stream)
    {
        var blob = Container.GetAppendBlobReference(SanitizePath(filePath));
        blob.AppendFromStream(stream);
    }

    public void DeleteFile(string filePath)
    {
        var blob = Container.GetAppendBlobReference(SanitizePath(filePath));
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
            Container.GetAppendBlobReference(((CloudAppendBlob)blob).Name).DeleteIfExists();
        }
    }

    public override IVirtualFile? GetFile(string virtualPath)
    {
        var filePath = SanitizePath(virtualPath);

        var blob = Container.GetAppendBlobReference(filePath);
        if (!blob.Exists()) return null;

        return new AzureAppendBlobVirtualFile(this, GetDirectory(GetDirPath(virtualPath))).Init(blob);
    }

    public override IVirtualDirectory GetDirectory(string? virtualPath)
    {
        return new AzureAppendBlobVirtualDirectory(this, virtualPath);
    }

    public override bool DirectoryExists(string virtualPath)
    {
        var ret = ((AzureAppendBlobVirtualDirectory)GetDirectory(virtualPath)).Exists();
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

    public string? GetDirPath(CloudAppendBlob blob) => GetDirPath(blob.Parent?.Prefix);

    public IEnumerable<AzureAppendBlobVirtualFile> EnumerateFiles(string? dirPath = null)
    {
        return Container.ListBlobs(dirPath == null ? null : dirPath + this.RealPathSeparator, useFlatBlobListing:true)
            .OfType<CloudAppendBlob>()
            .Select(q => new AzureAppendBlobVirtualFile(this, new AzureAppendBlobVirtualDirectory(this, GetDirPath(q))).Init(q));
    }

    public IEnumerable<AzureAppendBlobVirtualFile> GetImmediateFiles(string? fromDirPath)
    {
        var dir = new AzureAppendBlobVirtualDirectory(this, fromDirPath);

        return Container.ListBlobs((fromDirPath == null) ? null : fromDirPath + this.RealPathSeparator)
            .OfType<CloudAppendBlob>()
            .Select(q => new AzureAppendBlobVirtualFile(this, dir).Init(q));
    }

    public override string? SanitizePath(string filePath)
    {
        var sanitizedPath = string.IsNullOrEmpty(filePath)
            ? null
            : (filePath[0] == VirtualPathSeparator[0] ? filePath.Substring(1) : filePath);

        return sanitizedPath?.Replace('\\', VirtualPathSeparator[0]);
    }
}