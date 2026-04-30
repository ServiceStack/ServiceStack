using ServiceStack.IO;
using System;
using System.IO;
using ServiceStack.VirtualPath;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ServiceStack.Azure.Storage;

public class AzureBlobVirtualFile : AbstractVirtualFileBase
{
    private readonly AzureBlobVirtualFiles pathProvider;

    public BlobClient Blob { get; private set; }
    private BlobProperties properties;

    public AzureBlobVirtualFile(AzureBlobVirtualFiles owningProvider, IVirtualDirectory directory)
        : base(owningProvider, directory)
    {
        this.pathProvider = owningProvider;
    }

    public AzureBlobVirtualFile Init(BlobClient blob, BlobProperties properties)
    {
        Blob = blob;
        this.properties = properties;
        return this;
    }

    public override DateTime LastModified => properties.LastModified.UtcDateTime;

    public override long Length => properties.ContentLength;

    public override string Name => Blob.Name.Contains(pathProvider.VirtualPathSeparator)
        ? Blob.Name.SplitOnLast(pathProvider.VirtualPathSeparator)[1]
        : Blob.Name;

    public string FilePath => Blob.Name;

    public string ContentType => properties.ContentType;

    public override string VirtualPath => FilePath;

    public string DirPath => base.Directory.VirtualPath;

    public override Stream OpenRead() => Blob.OpenRead();

    public override void Refresh()
    {
        var blob = pathProvider.Container.GetBlobClient(Blob.Name);
        if (!blob.Exists().Value)
            return;

        Init(blob, blob.GetProperties().Value);
    }
}
