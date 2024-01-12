using ServiceStack.IO;
using System;
using System.IO;
using ServiceStack.VirtualPath;
using Microsoft.WindowsAzure.Storage.Blob;

namespace ServiceStack.Azure.Storage;

public class AzureAppendBlobVirtualFile : AbstractVirtualFileBase
{
    private readonly AzureAppendBlobVirtualFiles pathProvider;
    private readonly CloudBlobContainer container;

    public CloudAppendBlob Blob { get; private set; }

    public AzureAppendBlobVirtualFile(AzureAppendBlobVirtualFiles owningProvider, IVirtualDirectory directory)
        : base(owningProvider, directory)
    {
        this.pathProvider = owningProvider;
        this.container = pathProvider.Container;
    }

    public AzureAppendBlobVirtualFile Init(CloudAppendBlob blob)
    {
        this.Blob = blob;
        return this;
    }

    public override DateTime LastModified => Blob.Properties.LastModified?.UtcDateTime ?? DateTime.MinValue;

    public override long Length => Blob.Properties.Length;

    public override string Name => Blob.Name.Contains(pathProvider.VirtualPathSeparator)
        ? Blob.Name.SplitOnLast(pathProvider.VirtualPathSeparator)[1]
        : Blob.Name;

    public string FilePath => Blob.Name;

    public string ContentType => Blob.Properties.ContentType;

    public override string VirtualPath => FilePath;

    public string? DirPath => base.Directory.VirtualPath;

    public override Stream OpenRead()
    {
        return Blob.OpenRead();
    }

    public override void Refresh()
    {
        var blob = pathProvider.Container.GetAppendBlobReference(Blob.Name);
        if (!blob.Exists()) 
            return;

        Init(blob);
    }
}