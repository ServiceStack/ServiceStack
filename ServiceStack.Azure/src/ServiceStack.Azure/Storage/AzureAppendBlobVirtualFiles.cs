using ServiceStack.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using ServiceStack.VirtualPath;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

namespace ServiceStack.Azure.Storage;

public class AzureAppendBlobVirtualFiles : AbstractVirtualPathProviderBase, IVirtualFiles
{
    public BlobContainerClient Container { get; }

    private readonly AzureAppendBlobVirtualDirectory rootDirectory;

    public override IVirtualDirectory RootDirectory => rootDirectory;
    public override string VirtualPathSeparator => "/";
    public override string RealPathSeparator => "/";

    public AzureAppendBlobVirtualFiles(string connectionString, string containerName)
    {
        Container = new BlobContainerClient(connectionString, containerName);
        Container.CreateIfNotExists();
        rootDirectory = new AzureAppendBlobVirtualDirectory(this, null);
    }

    public AzureAppendBlobVirtualFiles(BlobContainerClient container)
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
        var blob = Container.GetAppendBlobClient(SanitizePath(filePath));
        blob.DeleteIfExists();
        blob.Create(new AppendBlobCreateOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = MimeTypes.GetMimeType(filePath) }
        });
        blob.AppendBlock(new MemoryStream(Encoding.UTF8.GetBytes(textContents)));
    }

    public void WriteFile(string filePath, Stream stream)
    {
        var blob = Container.GetAppendBlobClient(SanitizePath(filePath));
        blob.DeleteIfExists();
        blob.Create(new AppendBlobCreateOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = MimeTypes.GetMimeType(filePath) }
        });
        blob.AppendBlock(stream);
    }

    public void WriteFiles(IEnumerable<IVirtualFile> files, Func<IVirtualFile, string>? toPath = null)
    {
        this.CopyFrom(files, toPath);
    }

    public void AppendFile(string filePath, string textContents)
    {
        Container.GetAppendBlobClient(SanitizePath(filePath))
            .AppendBlock(new MemoryStream(Encoding.UTF8.GetBytes(textContents)));
    }

    public void AppendFile(string filePath, Stream stream)
    {
        Container.GetAppendBlobClient(SanitizePath(filePath)).AppendBlock(stream);
    }

    public void DeleteFile(string filePath)
    {
        Container.GetAppendBlobClient(SanitizePath(filePath)).Delete();
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
        
        if (!dirPath.EndsWith(RealPathSeparator)) dirPath = $"{dirPath}{RealPathSeparator}";

        foreach (var item in AzureBlobVirtualFilesHelpers.ListBlobs(Container, dirPath))
            Container.GetAppendBlobClient(item.Name).DeleteIfExists();
    }

    public override IVirtualFile? GetFile(string virtualPath)
    {
        var filePath = SanitizePath(virtualPath);
        var blob = Container.GetAppendBlobClient(filePath);
        if (!blob.Exists().Value)
            return null;

        var props = blob.GetProperties().Value;
        return new AzureAppendBlobVirtualFile(this, GetDirectory(GetDirPath(virtualPath))).Init(blob, props);
    }

    public override IVirtualDirectory GetDirectory(string? virtualPath) =>
        new AzureAppendBlobVirtualDirectory(this, virtualPath);

    public override bool DirectoryExists(string virtualPath) =>
        ((AzureAppendBlobVirtualDirectory)GetDirectory(virtualPath)).Exists();

    public string? GetDirPath(string? filePath) =>
        AzureBlobVirtualFilesHelpers.GetDirPath(filePath);

    public IEnumerable<AzureAppendBlobVirtualFile> EnumerateFiles(string? dirPath = null)
    {
        var prefix = dirPath == null ? null : dirPath + RealPathSeparator;
        return AzureBlobVirtualFilesHelpers.ListBlobs(Container, prefix)
            .Select(item =>
            {
                var blobClient = Container.GetAppendBlobClient(item.Name);
                var props = AzureBlobVirtualFilesHelpers.MakeBlobProperties(item, item.Properties.CreatedOn ?? default);
                return new AzureAppendBlobVirtualFile(this, new AzureAppendBlobVirtualDirectory(this, GetDirPath(item.Name))).Init(blobClient, props);
            });
    }

    public IEnumerable<AzureAppendBlobVirtualFile> GetImmediateFiles(string? fromDirPath)
    {
        var prefix = fromDirPath == null ? null : fromDirPath + RealPathSeparator;
        var dir = new AzureAppendBlobVirtualDirectory(this, fromDirPath);

        return AzureBlobVirtualFilesHelpers.ListBlobsByHierarchy(Container, prefix)
            .Where(static x => x.IsBlob)
            .Select(x =>
            {
                var blobClient = Container.GetAppendBlobClient(x.Blob.Name);
                var props = AzureBlobVirtualFilesHelpers.MakeBlobProperties(x.Blob, x.Blob.Properties.CreatedOn ?? default);
                return new AzureAppendBlobVirtualFile(this, dir).Init(blobClient, props);
            });
    }

    public override string? SanitizePath(string filePath) =>
        AzureBlobVirtualFilesHelpers.SanitizePath(filePath);
}
