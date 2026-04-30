using ServiceStack.IO;
using System;
using System.IO;
using ServiceStack.VirtualPath;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Blobs.Models;

namespace ServiceStack.Azure.Storage;

public class AzureAppendBlobVirtualFile : AbstractVirtualFileBase
{
    private readonly AzureAppendBlobVirtualFiles pathProvider;

    public AppendBlobClient Blob { get; private set; }
    private BlobProperties properties;

    public AzureAppendBlobVirtualFile(AzureAppendBlobVirtualFiles owningProvider, IVirtualDirectory directory)
        : base(owningProvider, directory)
    {
        this.pathProvider = owningProvider;
    }

    public AzureAppendBlobVirtualFile Init(AppendBlobClient blob, BlobProperties properties)
    {
        Blob = blob;
        this.properties = properties;
        return this;
    }

    public override DateTime LastModified =>
        (properties.CreatedOn != default ? properties.CreatedOn : properties.LastModified).UtcDateTime;

    public override long Length => properties.ContentLength;

    public override string Name => Blob.Name.Contains(pathProvider.VirtualPathSeparator)
        ? Blob.Name.SplitOnLast(pathProvider.VirtualPathSeparator)[1]
        : Blob.Name;

    public string FilePath => Blob.Name;

    public string ContentType => properties.ContentType;

    public override string VirtualPath => FilePath;

    public string? DirPath => base.Directory.VirtualPath;

    public override Stream OpenRead() => Blob.OpenRead();

    public override void Refresh()
    {
        var blob = pathProvider.Container.GetAppendBlobClient(Blob.Name);
        if (!blob.Exists().Value)
            return;

        Init(blob, blob.GetProperties().Value);
    }
}
