using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon.S3;
using Amazon.S3.Model;

namespace ServiceStack.Aws.FileStorage
{
    public class S3FileStorageProvider : BaseFileStorageProvider, IDisposable
    {
        private readonly S3ConnectionFactory s3ConnectionFactory;
        private IAmazonS3 s3Client;
        private static readonly FileSystemStorageProvider localFs = FileSystemStorageProvider.Instance;

        public S3FileStorageProvider(S3ConnectionFactory s3ConnFactory)
        {
            s3ConnectionFactory = s3ConnFactory ?? throw new ArgumentNullException(nameof(s3ConnFactory));
        }

        public void Dispose()
        {
            if (s3Client == null)
                return;

            try
            {
                s3Client.Dispose();
                s3Client = null;
            }
            catch { }
        }

        private IAmazonS3 S3Client => s3Client ?? (s3Client = s3ConnectionFactory.GetClient());

        public override char DirectorySeparatorCharacter => '/';

        public override void Download(FileSystemObject thisFso, FileSystemObject downloadToFso)
        {
            GetToLocalFile(thisFso, downloadToFso);
        }

        private bool IsMissingObjectException(AmazonS3Exception s3x)
        {
            return s3x.ErrorCode.Equals("NoSuchBucket", StringComparison.OrdinalIgnoreCase) ||
                   s3x.ErrorCode.Equals("NoSuchKey", StringComparison.OrdinalIgnoreCase);
        }

        public override Stream GetStream(FileSystemObject fso)
        {
            var bki = new S3BucketKeyInfo(fso.FullName);

            var request = new GetObjectRequest
            {
                BucketName = bki.BucketName,
                Key = bki.Key
            };

            try
            {
                return S3Client.GetObject(request).ResponseStream;
            }
            catch (AmazonS3Exception s3x)
            {
                if (IsMissingObjectException(s3x))
                    return null;

                throw;
            }
        }

        public override byte[] Get(FileSystemObject fso)
        {
            using (var stream = GetStream(fso))
            {
                return stream?.ToBytes();
            }
        }

        public override void Store(byte[] bytes, FileSystemObject fso)
        {
            using (var byteStream = new MemoryStream(bytes))
            {
                Store(byteStream, fso);
            }
        }

        public override void Store(Stream stream, FileSystemObject fso)
        {
            var bki = new S3BucketKeyInfo(fso);

            var attemptedBucketCreate = false;

            do
            {
                try
                {
                    var request = new PutObjectRequest
                    {
                        BucketName = bki.BucketName,
                        Key = bki.Key,
                        InputStream = stream,
                        StorageClass = S3StorageClass.Standard
                    };

                    S3Client.PutObject(request);

                    break;
                }
                catch (AmazonS3Exception s3x)
                {
                    if (!attemptedBucketCreate && IsMissingObjectException(s3x))
                    {
                        CreateFolder(fso.FolderName);
                        attemptedBucketCreate = true;
                        continue;
                    }

                    throw;
                }

            } while (true);
        }

        public override void Store(FileSystemObject localFileSystemFso, FileSystemObject targetFso)
        {
            using (var stream = new FileStream(localFileSystemFso.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Store(stream, targetFso);
            }
        }

        public override void Delete(FileSystemObject fso)
        {
            var bki = new S3BucketKeyInfo(fso);
            Delete(bki);
        }

        public override void Delete(IEnumerable<FileSystemObject> fsos)
        {
            foreach (var fso in fsos)
            {
                Delete(fso);
            }
        }

        private void Delete(S3BucketKeyInfo bki)
        {
            var request = new DeleteObjectRequest
            {
                BucketName = bki.BucketName,
                Key = bki.Key
            };

            try
            {
                S3Client.DeleteObject(request);
            }
            catch (AmazonS3Exception s3x)
            {
                if (IsMissingObjectException(s3x))
                {
                    return;
                }

                throw;
            }
        }

        private bool TreatAsS3Provider(IFileStorageProvider targetProvider)
        {
            if (targetProvider == null)
            {
                return true;
            }

            var s3Provider = targetProvider as S3FileStorageProvider;

            return s3Provider != null;
        }

        public override void Copy(FileSystemObject thisFso, FileSystemObject targetFso, IFileStorageProvider targetProvider = null)
        {   // If targetProvider is null or is an S3 provider, we are copying within S3, otherwise we
            // are copying across providers
            if (TreatAsS3Provider(targetProvider))
            {
                CopyInS3(thisFso, targetFso);
                return;
            }

            // Copying across providers. With S3 in this case, need to basically download the file
            // to the local file system first, then copy from there to the target provder

            var localFile = new FileSystemObject(Path.Combine(Path.GetTempPath(), targetFso.FileNameAndExtension));

            try
            {
                GetToLocalFile(thisFso, localFile);
                targetProvider.Store(localFile, targetFso);
            }
            finally
            {
                localFs.Delete(localFile);
            }
        }

        private void CopyInS3(FileSystemObject sourceFso, FileSystemObject targetFso)
        {
            if (sourceFso.Equals(targetFso))
            {
                return;
            }

            var sourceBki = new S3BucketKeyInfo(sourceFso);
            var targetBki = new S3BucketKeyInfo(targetFso);

            var request = new CopyObjectRequest
            {
                SourceBucket = sourceBki.BucketName,
                SourceKey = sourceBki.Key,
                DestinationBucket = targetBki.BucketName,
                DestinationKey = targetBki.Key
            };

            S3Client.CopyObject(request);
        }

        private void GetToLocalFile(FileSystemObject fso, FileSystemObject downloadToFso)
        {
            var bki = new S3BucketKeyInfo(fso.FullName);

            var request = new GetObjectRequest
            {
                BucketName = bki.BucketName,
                Key = bki.Key
            };

            localFs.CreateFolder(downloadToFso.FolderName);

            localFs.Delete(downloadToFso);

            using (var response = S3Client.GetObject(request))
            {
                response.WriteResponseStreamToFile(downloadToFso.FullName, append:false);
            }

        }

        public override void Move(FileSystemObject sourceFso, FileSystemObject targetFso, IFileStorageProvider targetProvider = null)
        {
            if (TreatAsS3Provider(targetProvider))
            {
                MoveInS3(sourceFso, targetFso);
                return;
            }

            // Moving across providers. With S3 in this case, need to basically download the file
            // to the local file system first, then copy from there to the target provder

            var localFile = new FileSystemObject(Path.Combine(Path.GetTempPath(), targetFso.FileNameAndExtension));

            try
            {
                GetToLocalFile(sourceFso, localFile);

                targetProvider.Store(localFile, targetFso);

                Delete(sourceFso);
            }
            finally
            {
                localFs.Delete(localFile);
            }

        }

        private void MoveInS3(FileSystemObject sourceFso, FileSystemObject targetFso)
        {
            if (sourceFso.Equals(targetFso))
            {
                return;
            }

            // Copy, then delete
            CopyInS3(sourceFso, targetFso);
            Delete(sourceFso);
        }

        public override bool Exists(FileSystemObject fso)
        {
            var bki = new S3BucketKeyInfo(fso.FullName);

            try
            {
                var response = S3Client.GetObjectMetadata(new GetObjectMetadataRequest
                {
                    BucketName = bki.BucketName,
                    Key = bki.Key
                });
                return true;
            }
            catch (AmazonS3Exception ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return false;

                //status wasn't not found, so throw the exception
                throw;
            }
        }

        public override bool FolderExists(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            var bki = new S3BucketKeyInfo(path);

            // Folders aren't a thing in S3, buckets are the only things that actually matter more or less. Return true
            // if this is a bucket object and the bucket exists, otherwise false since it actually doesn't exist
            return bki.IsBucketObject && BucketExists(bki);
        }

        private bool BucketExists(S3BucketKeyInfo bki)
        {
            if (string.IsNullOrEmpty(bki.BucketName))
            {
                return false;
            }

            try
            {
                var bucketLocation = S3Client.GetBucketLocation(new GetBucketLocationRequest { 
                    BucketName = bki.BucketName
                });
                return true;
            }
            catch (AmazonS3Exception s3x)
            {
                if (IsMissingObjectException(s3x))
                {
                    return false;
                }

                throw;
            }

        }

        public override IEnumerable<string> ListFolder(string folderName, bool recursive = false, bool fileNamesOnly = false)
        {
            if (string.IsNullOrEmpty(folderName))
            {
                yield break;
            }

            string nextMarker = null;

            var bki = new S3BucketKeyInfo(folderName, terminateWithPathDelimiter: true);

            do
            {
                var listResponse = ListFolderResponse(bki, nextMarker);

                if (listResponse == null || listResponse.S3Objects == null)
                    break;

                var filesOnly = listResponse.S3Objects
                    .Select(o => new S3BucketKeyInfo(bki.BucketName, o))
                    .Where(b => !string.IsNullOrEmpty(b.FileName))
                    .Where(b => recursive
                        ? b.Prefix.StartsWith(bki.Prefix, StringComparison.Ordinal)
                        : b.Prefix.Equals(bki.Prefix, StringComparison.Ordinal))
                    .Select(b => fileNamesOnly
                        ? b.FileName
                        : b.ToString()
                    );

                foreach (var file in filesOnly)
                {
                    yield return file;
                }

                if (listResponse.IsTruncated)
                {
                    nextMarker = listResponse.NextMarker;
                }
                else
                {
                    break;
                }

            } while (true);

        }

        private List<S3Object> ListFolder(S3BucketKeyInfo bki)
        {
            var listResponse = ListFolderResponse(bki);

            return listResponse == null
                ? new List<S3Object>()
                : listResponse.S3Objects ?? new List<S3Object>();
        }

        private ListObjectsResponse ListFolderResponse(S3BucketKeyInfo bki, string nextMarker = null)
        {
            try
            {
                var listRequest = new ListObjectsRequest
                {
                    BucketName = bki.BucketName
                };

                if (!string.IsNullOrEmpty(nextMarker))
                {
                    listRequest.Marker = nextMarker;
                }

                if (bki.HasPrefix)
                {
                    listRequest.Prefix = bki.Prefix;
                }

                var listResponse = S3Client.ListObjects(listRequest);

                return listResponse;
            }
            catch (System.Xml.XmlException) { }
            catch (AmazonS3Exception s3x)
            {
                if (!IsMissingObjectException(s3x))
                {
                    throw;
                }
            }

            return null;
        }

        public override void DeleteFolder(string path, bool recursive)
        {
            try
            {
                DeleteFolderInternal(path, recursive);
            }
            catch (AmazonS3Exception s3x)
            {
                if (IsMissingObjectException(s3x))
                    return;

                throw;
            }
        }

        private void DeleteFolderInternal(string path, bool recursive)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var bki = new S3BucketKeyInfo(path, terminateWithPathDelimiter: true);

            if (recursive)
            {
                while (true)
                {
                    var objects = ListFolder(bki);

                    if (!objects.Any())
                        break;

                    var keys = objects.Select(o => new KeyVersion
                    {
                        Key = o.Key
                    })
                    .ToList();

                    var deleteObjectsRequest = new DeleteObjectsRequest
                    {
                        BucketName = bki.BucketName,
                        Quiet = true,
                        Objects = keys
                    };

                    S3Client.DeleteObjects(deleteObjectsRequest);
                }
            }
            else if (!bki.IsBucketObject)
            {
                var deleteObjectRequest = new DeleteObjectRequest
                {
                    BucketName = bki.BucketName,
                    Key = bki.Key
                };

                S3Client.DeleteObject(deleteObjectRequest);
            }

            if (bki.IsBucketObject)
            {
                var request = new DeleteBucketRequest
                {
                    BucketName = bki.BucketName,
                    UseClientRegion = true
                };

                S3Client.DeleteBucket(request);
            }
        }

        public override void CreateFolder(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            var bki = new S3BucketKeyInfo(path);

            var request = new PutBucketRequest
            {
                BucketName = bki.BucketName,
                UseClientRegion = true
            };

            S3Client.PutBucket(request);
        }
    }
}
