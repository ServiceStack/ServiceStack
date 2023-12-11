using System.Net.Http.Headers;
using Google.Cloud.Storage.V1;
using ServiceStack.IO;
using ServiceStack.VirtualPath;
using Object = Google.Apis.Storage.v1.Data.Object;

namespace ServiceStack.GoogleCloud;

public class GoogleCloudVirtualFile : AbstractVirtualFileBase
{
    private GoogleCloudVirtualFiles PathProvider { get; set; }

    public StorageClient Client => PathProvider.StorageClient;

    public string BucketName => PathProvider.BucketName;

    public GoogleCloudVirtualFile(GoogleCloudVirtualFiles pathProvider, IVirtualDirectory directory)
        : base(pathProvider, directory)
    {
        this.PathProvider = pathProvider;
    }

    public string DirPath => base.Directory.VirtualPath;

    public string? FilePath { get; set; }

    public string? ContentType { get; set; }

    public override string? Name => GoogleCloudVirtualFiles.GetFileName(FilePath);

    public override string? VirtualPath => FilePath;

    public DateTime FileLastModified { get; set; }

    public override DateTime LastModified => FileLastModified;

    public override long Length => ContentLength;

    public long ContentLength { get; set; }

    public string? Etag { get; set; }

    public Stream Stream { get; set; }

    public GoogleCloudVirtualFile Init(Object response)
    {
        FilePath = response.Name;
        ContentType = response.ContentType;
        FileLastModified = response.Updated ?? DateTime.UtcNow;
        ContentLength = (long)(response.Size ?? 0);
        Etag = response.ETag;
        Stream = new MemoryStream();
        Client.DownloadObject(response, Stream);
        return this;
    }

    public override Stream OpenRead()
    {
        if (Stream is not { CanRead: true })
        {
            var response = Client.GetObject(bucket:BucketName, objectName:FilePath);
            Init(response);
        }
        if (Stream is { CanSeek: true })
        {
            Stream.Position = 0;
        }
        return Stream;
    }

    public override void Refresh()
    {
        try
        {
            // Optimize with ETag
            var response = Client.GetObject(bucket:BucketName, objectName:FilePath);
            Init(response);
        }
        catch (Exception ex)
        {
            // if (ex.StatusCode != HttpStatusCode.NotModified)
            //     throw;
        }
    }

    public override async Task WritePartialToAsync(Stream toStream, long start, long end, CancellationToken token = default)
    {
        await Client.DownloadObjectAsync(bucket: BucketName, objectName: FilePath, destination: toStream,
            new DownloadObjectOptions {
                Range = new RangeHeaderValue(start, end)
            }, token);
    }
}
