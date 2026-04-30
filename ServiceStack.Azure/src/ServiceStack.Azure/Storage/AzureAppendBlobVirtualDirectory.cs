using ServiceStack.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.VirtualPath;
using Azure.Storage.Blobs.Specialized;

namespace ServiceStack.Azure.Storage;

public class AzureAppendBlobVirtualDirectory : AbstractVirtualDirectoryBase
{
    public AzureAppendBlobVirtualFiles PathProvider { get; }

    public AzureAppendBlobVirtualDirectory(AzureAppendBlobVirtualFiles pathProvider, string? dirPath)
        : base(pathProvider)
    {
        this.PathProvider = pathProvider;
        this.DirPath = dirPath;

        if (dirPath == "/" || dirPath.IsNullOrEmpty())
            return;

        var separatorIndex = dirPath!.LastIndexOf(pathProvider.RealPathSeparator, StringComparison.Ordinal);

        ParentDirectory = new AzureAppendBlobVirtualDirectory(pathProvider,
            separatorIndex == -1 ? string.Empty : dirPath.Substring(0, separatorIndex));
    }

    public string? DirPath { get; set; }

    [Obsolete("Use DirPath")]
    public string? DirectoryPath => DirPath;

    public override IEnumerable<IVirtualDirectory> Directories
    {
        get
        {
            var blobs = AzureBlobVirtualFilesHelpers.ListBlobsByHierarchy(PathProvider.Container, DirPath == null
                ? null
                : $"{DirPath}{PathProvider.RealPathSeparator}");
            
            return blobs
                .Where(static q => q.IsPrefix)
                .Select(q => 
                    new AzureAppendBlobVirtualDirectory(PathProvider, q.Prefix.Trim(PathProvider.RealPathSeparator[0])));
        }
    }

    public override DateTime LastModified => throw new NotImplementedException();

    public override IEnumerable<IVirtualFile> Files => PathProvider.GetImmediateFiles(this.DirPath);

    public bool Exists()
    {
        var prefix = DirPath == null ? null : $"{DirPath}/";
        return AzureBlobVirtualFilesHelpers.ListBlobs(PathProvider.Container, prefix).Any();
    }

    public override string? Name => DirPath?.SplitOnLast(PathProvider.RealPathSeparator).Last();

    public override string? VirtualPath => DirPath;

    public override IEnumerator<IVirtualNode> GetEnumerator() => throw new NotImplementedException();

    protected override IVirtualFile GetFileFromBackingDirectoryOrDefault(string fileName)
    {
        fileName = PathProvider.CombineVirtualPath(this.DirPath, PathProvider.SanitizePath(fileName));
        return PathProvider.GetFile(fileName);
    }

    protected override IEnumerable<IVirtualFile> GetMatchingFilesInDir(string globPattern)
    {
        var prefix = DirPath == null ? null : $"{DirPath}{PathProvider.RealPathSeparator}";
        return AzureBlobVirtualFilesHelpers.ListBlobsByHierarchy(PathProvider.Container, prefix)
            .Where(static x => x.IsBlob)
            .Where(x => x.Blob.Name.Glob(globPattern))
            .Select(x =>
            {
                var blobClient = PathProvider.Container.GetAppendBlobClient(x.Blob.Name);
                var props = AzureBlobVirtualFilesHelpers.MakeBlobProperties(x.Blob, x.Blob.Properties.CreatedOn ?? default);
                return new AzureAppendBlobVirtualFile(PathProvider, this).Init(blobClient, props);
            });
    }

    protected override IVirtualDirectory GetDirectoryFromBackingDirectoryOrDefault(string directoryName) =>
        new AzureAppendBlobVirtualDirectory(this.PathProvider, PathProvider.SanitizePath(DirPath.CombineWith(directoryName)));

    public override IEnumerable<IVirtualFile> GetAllMatchingFiles(string globPattern, int maxDepth = int.MaxValue)
    {
        if (IsRoot)
        {
            return PathProvider.EnumerateFiles().Where(x =>
                (x.DirPath == null || x.DirPath.CountOccurrencesOf(VirtualPathProvider.RealPathSeparator) < maxDepth - 1)
                && x.Name.Glob(globPattern));
        }

        return PathProvider.EnumerateFiles(DirPath).Where(x =>
            x.DirPath != null
            && x.DirPath.CountOccurrencesOf(VirtualPathProvider.RealPathSeparator) < maxDepth - 1
            && x.DirPath.StartsWith(DirPath)
            && x.Name.Glob(globPattern));
    }
}
