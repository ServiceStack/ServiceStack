using Amazon.S3;
using Amazon.S3.Model;
using ServiceStack.Aws;
using ServiceStack.Logging;
using ServiceStack.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.IO;

/// <summary>
/// Custom S3VirtualFiles to work around compatibility issues with Cloudflare R2 services
/// </summary>
public class R2VirtualFiles : S3VirtualFiles
{
    public static ILog Log = LogManager.GetLogger(typeof(R2VirtualFiles));

    public R2VirtualFiles(IAmazonS3 client, string bucketName) : base(client, bucketName) {}

    public override void WriteFile(string filePath, Stream stream)
    {
        AmazonS3.PutObject(new PutObjectRequest
        {
            Key = SanitizePath(filePath),
            BucketName = BucketName,
            InputStream = stream,
            DisablePayloadSigning = true,
        });
    }

    public override void WriteFile(string filePath, string contents)
    {
        AmazonS3.PutObject(new PutObjectRequest
        {
            Key = SanitizePath(filePath),
            BucketName = BucketName,
            ContentBody = contents,
            DisablePayloadSigning = true,
        });
    }

    public override async Task WriteFileAsync(string path, object contents, CancellationToken token = default)
    {
        try
        {
            // need to buffer otherwise hangs when trying to send an uploaded file stream (depends on provider)
            var buffer = contents is not MemoryStream;
            var fileContents = await FileContents.GetAsync(contents, buffer);
            if (fileContents?.Stream != null)
            {
                await AmazonS3.PutObjectAsync(new PutObjectRequest
                {
                    Key = SanitizePath(path),
                    BucketName = BucketName,
                    InputStream = fileContents.Stream,
                    DisablePayloadSigning = true,
                }, token).ConfigAwait();
            }
            else if (fileContents?.Text != null)
            {
                await AmazonS3.PutObjectAsync(new PutObjectRequest
                {
                    Key = SanitizePath(path),
                    BucketName = BucketName,
                    ContentBody = fileContents.Text,
                    DisablePayloadSigning = true,
                }, token).ConfigAwait();
            }
            else throw new NotSupportedException($"Unknown File Content Type: {contents.GetType().Name}");

            if (buffer && fileContents.Stream != null)
                using (fileContents.Stream) {}
        }
        catch (Exception e)
        {
            Log.Error(e, "Could not write file to {0}", path);
        }
    }
}