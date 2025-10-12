// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using ServiceStack.IO;
using ServiceStack.Text;
using ServiceStack.VirtualPath;

namespace ServiceStack.Aws.S3;

public class S3VirtualFile : AbstractVirtualFileBase
{
    private S3VirtualFiles PathProvider { get; set; }

    public IAmazonS3 Client => PathProvider.AmazonS3;

    public string BucketName => PathProvider.BucketName;

    public S3VirtualFile(S3VirtualFiles pathProvider, IVirtualDirectory directory)
        : base(pathProvider, directory)
    {
        this.PathProvider = pathProvider;
    }

    public string DirPath => base.Directory.VirtualPath;

    public string FilePath { get; set; }

    public string ContentType { get; set; }

    public override string Name => S3VirtualFiles.GetFileName(FilePath);

    public override string VirtualPath => FilePath;

    public DateTime? FileLastModified { get; set; }

    public override DateTime LastModified => FileLastModified ?? DateTime.MinValue;

    public override long Length => ContentLength;

    public long ContentLength { get; set; }

    public string Etag { get; set; }

    public Stream Stream { get; set; }

    public S3VirtualFile Init(GetObjectResponse response)
    {
        FilePath = response.Key;
        ContentType = response.Headers.ContentType;
        FileLastModified = response.LastModified;
        ContentLength = response.Headers.ContentLength;
        Etag = response.ETag;
        Stream = response.ResponseStream;
        return this;
    }

    public override Stream OpenRead()
    {
        if (Stream is not { CanRead: true })
        {
            var response = Client.GetObject(new GetObjectRequest
            {
                Key = FilePath,
                BucketName = BucketName,
            });
            Init(response);
        }

        return Stream;
    }

    public override void Refresh()
    {
        try
        {
            var response = Client.GetObject(new GetObjectRequest
            {
                Key = FilePath,
                BucketName = BucketName,
                EtagToNotMatch = Etag,
            });
            Init(response);
        }
        catch (AmazonS3Exception ex)
        {
            if (ex.StatusCode != HttpStatusCode.NotModified)
                throw;
        }
    }

    public override async Task WritePartialToAsync(Stream toStream, long start, long end, CancellationToken token = default)
    {
        var response = await Client.GetObjectAsync(new GetObjectRequest
        {
            Key = FilePath,
            BucketName = BucketName,
            ByteRange = new ByteRange(start, end)
        }, token);
        Init(response);

        await response.ResponseStream.WriteToAsync(toStream, token).ConfigAwait();
    }
}
