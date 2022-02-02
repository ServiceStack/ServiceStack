using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using ServiceStack.Aws;
using ServiceStack.Aws.S3;
using ServiceStack.VirtualPath;

namespace ServiceStack.IO
{
    public partial class S3VirtualFiles : AbstractVirtualPathProviderBase, IVirtualFiles
    {
        public const int MultiObjectLimit = 1000;

        public IAmazonS3 AmazonS3 { get; private set; }
        public string BucketName { get; private set; }
        private readonly S3VirtualDirectory rootDirectory;

        public S3VirtualFiles(IAmazonS3 client, string bucketName)
        {
            this.AmazonS3 = client;
            this.BucketName = bucketName;
            this.rootDirectory = new S3VirtualDirectory(this, null, null);
        }

        public const char DirSep = '/';

        public override IVirtualDirectory RootDirectory => rootDirectory;

        public override string VirtualPathSeparator => "/";

        public override string RealPathSeparator => "/";

        protected override void Initialize() {}

        public override IVirtualFile GetFile(string virtualPath)
        {
            if (string.IsNullOrEmpty(virtualPath))
                return null;

            var filePath = SanitizePath(virtualPath);
            try
            {
                var response = AmazonS3.GetObject(new GetObjectRequest
                {
                    Key = filePath,
                    BucketName = BucketName,
                });

                var dirPath = GetDirPath(filePath);
                return new S3VirtualFile(this, new S3VirtualDirectory(this, dirPath, GetParentDirectory(dirPath))).Init(response);
            }
            catch (AmazonS3Exception ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                    return null;

                throw;
            }
        }

        private S3VirtualDirectory GetParentDirectory(string dirPath)
        {
            if (string.IsNullOrEmpty(dirPath))
                return null;

            var parentDir = GetDirPath(dirPath.TrimEnd(DirSep));
            return parentDir != null
                ? new S3VirtualDirectory(this, dirPath, GetParentDirectory(parentDir))
                : (S3VirtualDirectory)RootDirectory;
        }

        public override IVirtualDirectory GetDirectory(string virtualPath)
        {
            if (virtualPath == null)
                return null;

            var dirPath = SanitizePath(virtualPath);
            if (string.IsNullOrEmpty(dirPath))
                return RootDirectory;

            var seekPath = dirPath[dirPath.Length - 1] != DirSep
                ? dirPath + DirSep
                : dirPath;

            var response = AmazonS3.ListObjects(new ListObjectsRequest
            {
                BucketName = BucketName,
                Prefix = seekPath,
                MaxKeys = 1,
            });

            if (response.S3Objects.Count == 0)
                return null;

            return new S3VirtualDirectory(this, dirPath, GetParentDirectory(dirPath));
        }

        public override bool DirectoryExists(string virtualPath)
        {
            return GetDirectory(virtualPath) != null;
        }

        public override bool FileExists(string virtualPath)
        {
            return GetFile(virtualPath) != null;
        }

        public void WriteFile(string filePath, string contents)
        {
            AmazonS3.PutObject(new PutObjectRequest
            {
                Key = SanitizePath(filePath),
                BucketName = BucketName,
                ContentBody = contents,
            });
        }

        public void WriteFile(string filePath, Stream stream)
        {
            AmazonS3.PutObject(new PutObjectRequest
            {
                Key = SanitizePath(filePath),
                BucketName = BucketName,
                InputStream = stream,
            });
        }

        public void WriteFiles(IEnumerable<IVirtualFile> files, Func<IVirtualFile, string> toPath = null)
        {
            this.CopyFrom(files, toPath);
        }

        public void AppendFile(string filePath, string textContents)
        {
            throw new NotImplementedException("S3 doesn't support appending to files");
        }

        public void AppendFile(string filePath, Stream stream)
        {
            throw new NotImplementedException("S3 doesn't support appending to files");
        }

        public void DeleteFile(string filePath)
        {
            filePath = SanitizePath(filePath);
            AmazonS3.DeleteObject(new DeleteObjectRequest {
                BucketName = BucketName,
                Key = filePath,
            });
        }

        public void DeleteFiles(IEnumerable<string> filePaths)
        {
            var batches = filePaths
                .BatchesOf(MultiObjectLimit);

            foreach (var batch in batches)
            {
                var request = new DeleteObjectsRequest {
                    BucketName = BucketName,
                };

                foreach (var filePath in batch)
                {
                    request.AddKey(filePath);
                }

                AmazonS3.DeleteObjects(request);
            }
        }

        public void DeleteFolder(string dirPath)
        {
            dirPath = SanitizePath(dirPath);
            var nestedFiles = EnumerateFiles(dirPath).Map(x => x.FilePath);
            DeleteFiles(nestedFiles);
        }

        public IEnumerable<S3VirtualFile> EnumerateFiles(string prefix = null)
        {
            var response = AmazonS3.ListObjects(new ListObjectsRequest
            {
                BucketName = BucketName,
                Prefix = prefix,
            });

            foreach (var file in response.S3Objects)
            {
                var filePath = SanitizePath(file.Key);

                var dirPath = GetDirPath(filePath);
                yield return new S3VirtualFile(this, new S3VirtualDirectory(this, dirPath, GetParentDirectory(dirPath)))
                {
                    FilePath = filePath,
                    ContentLength = file.Size,
                    FileLastModified = file.LastModified,
                    Etag = file.ETag,
                };
            }
        }

        public override IEnumerable<IVirtualFile> GetAllFiles()
        {
            return EnumerateFiles();
        }

        public IEnumerable<S3VirtualDirectory> GetImmediateDirectories(string fromDirPath)
        {
            var dirPaths = EnumerateFiles(fromDirPath)
                .Map(x => x.DirPath)
                .Distinct()
                .Map(x => GetImmediateSubDirPath(fromDirPath, x))
                .Where(x => x != null)
                .Distinct();

            var parentDir = GetParentDirectory(fromDirPath);
            return dirPaths.Map(x => new S3VirtualDirectory(this, x, parentDir));
        }

        public IEnumerable<S3VirtualFile> GetImmediateFiles(string fromDirPath)
        {
            return EnumerateFiles(fromDirPath)
                .Where(x => x.DirPath == fromDirPath);
        }

        public string GetDirPath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return null;

            var lastDirPos = filePath.LastIndexOf(DirSep);
            return lastDirPos >= 0
                ? filePath.Substring(0, lastDirPos)
                : null;
        }

        public string GetImmediateSubDirPath(string fromDirPath, string subDirPath)
        {
            if (string.IsNullOrEmpty(subDirPath))
                return null;

            if (fromDirPath == null)
            {
                return subDirPath.CountOccurrencesOf(DirSep) == 0
                    ? subDirPath
                    : null;
            }

            if (!subDirPath.StartsWith(fromDirPath))
                return null;

            return fromDirPath.CountOccurrencesOf(DirSep) == subDirPath.CountOccurrencesOf(DirSep) - 1
                ? subDirPath
                : null;
        }

        public override string SanitizePath(string filePath)
        {
            var sanitizedPath = string.IsNullOrEmpty(filePath)
                ? null
                : (filePath[0] == DirSep ? filePath.Substring(1) : filePath);

            return sanitizedPath?.Replace('\\', DirSep);
        }

        public static string GetFileName(string filePath)
        {
            return filePath.SplitOnLast(DirSep).Last();
        }
    }

    public partial class S3VirtualFiles : IS3Client
    {
        public void ClearBucket()
        {
            var allFilePaths = EnumerateFiles()
                .Map(x => x.FilePath);

            DeleteFiles(allFilePaths);
        }
    }
    
}