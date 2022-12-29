using ServiceStack.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using Microsoft.WindowsAzure.Storage.Blob;
using ServiceStack.VirtualPath;

namespace ServiceStack.Azure.Storage
{
    public class AzureAppendBlobVirtualDirectory : AbstractVirtualDirectoryBase
    {
        public AzureAppendBlobVirtualFiles PathProvider { get; }

        public AzureAppendBlobVirtualDirectory(AzureAppendBlobVirtualFiles pathProvider, string dirPath)
            : base(pathProvider)
        {
            this.PathProvider = pathProvider;
            this.DirPath = dirPath;

            if (dirPath == "/" || dirPath.IsNullOrEmpty())
                return;

            var separatorIndex = dirPath.LastIndexOf(pathProvider.RealPathSeparator, StringComparison.Ordinal);

            ParentDirectory = new AzureAppendBlobVirtualDirectory(pathProvider,
                separatorIndex == -1 ? string.Empty : dirPath.Substring(0, separatorIndex));
        }

        public string DirPath { get; set; }

        [Obsolete("Use DirPath")]
        public string DirectoryPath => DirPath;

        public override IEnumerable<IVirtualDirectory> Directories
        {
            get
            {
                var blobs = PathProvider.Container.ListBlobs(DirPath == null
                    ? null
                    : DirPath + PathProvider.RealPathSeparator);

                return blobs.Where(q => q.GetType() == typeof(CloudBlobDirectory))
                    .Select(q =>
                    {
                        var blobDir = (CloudBlobDirectory)q;
                        return new AzureAppendBlobVirtualDirectory(PathProvider, blobDir.Prefix.Trim(PathProvider.RealPathSeparator[0]));
                    });
            }
        }

        public override DateTime LastModified => throw new NotImplementedException();

        public override IEnumerable<IVirtualFile> Files => PathProvider.GetImmediateFiles(this.DirPath);

        // Azure Blob storage directories only exist if there are contents beneath them
        public bool Exists()
        {
            var ret = PathProvider.Container
                .ListBlobs(this.DirPath, false)
                .Any(q => q.GetType() == typeof(CloudBlobDirectory));
            return ret;

        }

        public override string Name => DirPath?.SplitOnLast(PathProvider.RealPathSeparator).Last();

        public override string VirtualPath => DirPath;

        public override IEnumerator<IVirtualNode> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        protected override IVirtualFile GetFileFromBackingDirectoryOrDefault(string fileName)
        {
            fileName = PathProvider.CombineVirtualPath(this.DirPath, PathProvider.SanitizePath(fileName));
            return PathProvider.GetFile(fileName);
        }

        protected override IEnumerable<IVirtualFile> GetMatchingFilesInDir(string globPattern)
        {
            var dir = (this.DirPath == null) ? null : this.DirPath + PathProvider.RealPathSeparator;

            var ret = PathProvider.Container.ListBlobs(dir)
                      .Where(q => q.GetType() == typeof(CloudAppendBlob))
                      .Where(q =>
                      {
                          var x = ((CloudAppendBlob)q).Name.Glob(globPattern);
                          return x;
                      })
                      .Select(q => new AzureAppendBlobVirtualFile(PathProvider, this).Init(q as CloudAppendBlob));
            return ret;
        }

        protected override IVirtualDirectory GetDirectoryFromBackingDirectoryOrDefault(string directoryName)
        {
            return new AzureAppendBlobVirtualDirectory(this.PathProvider, PathProvider.SanitizePath(DirPath.CombineWith(directoryName)));
        }
        
        public override IEnumerable<IVirtualFile> GetAllMatchingFiles(string globPattern, int maxDepth = int.MaxValue)
        {
            if (IsRoot)
            {
                return PathProvider.EnumerateFiles().Where(x => 
                    (x.DirPath == null || x.DirPath.CountOccurrencesOf('/') < maxDepth-1)
                    && x.Name.Glob(globPattern));
            }
            
            return PathProvider.EnumerateFiles(DirPath).Where(x => 
                x.DirPath != null
                && x.DirPath.CountOccurrencesOf('/') < maxDepth-1
                && x.DirPath.StartsWith(DirPath)
                && x.Name.Glob(globPattern));
        }
    }
}