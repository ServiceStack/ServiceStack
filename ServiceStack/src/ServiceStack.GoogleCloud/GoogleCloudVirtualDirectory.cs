using Google.Cloud.Storage.V1;
using ServiceStack.IO;
using ServiceStack.Text;
using ServiceStack.VirtualPath;

namespace ServiceStack.GoogleCloud;

public class GoogleCloudVirtualDirectory : AbstractVirtualDirectoryBase
{
    internal GoogleCloudVirtualFiles PathProvider { get; private set; }

    public GoogleCloudVirtualDirectory(GoogleCloudVirtualFiles pathProvider, string? dirPath, GoogleCloudVirtualDirectory? parentDir)
        : base(pathProvider, parentDir)
    {
        this.PathProvider = pathProvider;
        this.DirPath = dirPath;
    }
        
    public DateTime DirLastModified { get; set; }

    public override DateTime LastModified => DirLastModified;

    public override IEnumerable<IVirtualFile> Files => PathProvider.GetImmediateFiles(DirPath);

    public override IEnumerable<IVirtualDirectory> Directories => PathProvider.GetImmediateDirectories(DirPath);

    public StorageClient Client => PathProvider.StorageClient;

    public string BucketName => PathProvider.BucketName;

    public string? DirPath { get; set; }

    public override string VirtualPath => DirPath ?? string.Empty;

    public override string? Name => DirPath?.SplitOnLast(MemoryVirtualFiles.DirSep).Last();

    public override IVirtualFile? GetFile(string virtualPath)
    {
        return PathProvider.GetFile(DirPath != null ? DirPath.CombineWith(virtualPath) : virtualPath);
    }

    public override IEnumerator<IVirtualNode> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    protected override IVirtualFile? GetFileFromBackingDirectoryOrDefault(string fileName)
    {
        return GetFile(fileName);
    }

    protected override IEnumerable<IVirtualFile> GetMatchingFilesInDir(string globPattern)
    {
        var matchingFilesInBackingDir = EnumerateFiles(globPattern);
        return matchingFilesInBackingDir;
    }

#if NET6_0_OR_GREATER
    protected virtual IAsyncEnumerable<GoogleCloudVirtualFile> GetMatchingFilesInDirAsync(string globPattern, CancellationToken token = default)
    {
        return EnumerateFilesAsync(globPattern, token);
    }
#endif
    
    public IEnumerable<GoogleCloudVirtualFile> EnumerateFiles(string pattern)
    {
        foreach (var file in PathProvider.GetImmediateFiles(DirPath).Where(f => f.Name?.Glob(pattern) == true))
        {
            yield return file;
        }
    }

#if NET6_0_OR_GREATER
    public async IAsyncEnumerable<GoogleCloudVirtualFile> EnumerateFilesAsync(string pattern, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token = default)
    {
        foreach (var file in await PathProvider.GetImmediateFilesAsync(DirPath, token).Where(f => f.Name?.Glob(pattern) == true).ToListAsync(token))
        {
            yield return file;
        }
    }
#endif

    protected override IVirtualDirectory GetDirectoryFromBackingDirectoryOrDefault(string directoryName)
    {
        var subDir = DirPath != null ? DirPath.CombineWith(directoryName) : directoryName;
        return new GoogleCloudVirtualDirectory(PathProvider, PathProvider.SanitizePath(subDir), this);
    }

    public void AddFile(string filePath, string contents)
    {
        PathProvider.WriteFile(DirPath != null ? DirPath.CombineWith(filePath) : filePath, contents);
    }

    public void AddFile(string filePath, Stream stream)
    {
        PathProvider.WriteFile(DirPath != null ? DirPath.CombineWith(filePath) : filePath, stream);
    }
        
    public override IEnumerable<IVirtualFile> GetAllMatchingFiles(string globPattern, int maxDepth = int.MaxValue)
    {
        if (IsRoot)
        {
            return PathProvider.EnumerateFiles().Where(x => 
                (x.DirPath == null || x.DirPath.CountOccurrencesOf('/') < maxDepth-1)
                && x.Name?.Glob(globPattern) == true);
        }
            
        return PathProvider.EnumerateFiles(DirPath).Where(x => 
            x.DirPath != null
            && x.DirPath.CountOccurrencesOf('/') < maxDepth-1
            && (DirPath == null || x.DirPath.StartsWith(DirPath))
            && x.Name?.Glob(globPattern) == true);
    }
        
#if NET6_0_OR_GREATER
    public virtual async Task<List<GoogleCloudVirtualFile>> GetAllMatchingFilesAsync(string globPattern, int maxDepth = int.MaxValue, 
        CancellationToken token = default)
    {
        if (IsRoot)
        {
            return await PathProvider.EnumerateFilesAsync(token:token).Where(x => 
                (x.DirPath == null || x.DirPath.CountOccurrencesOf('/') < maxDepth-1)
                && x.Name?.Glob(globPattern) == true).ToListAsync(token);
        }
            
        return await PathProvider.EnumerateFilesAsync(DirPath, token).Where(x => 
            x.DirPath != null
            && x.DirPath.CountOccurrencesOf('/') < maxDepth-1
            && (DirPath == null || x.DirPath.StartsWith(DirPath))
            && x.Name?.Glob(globPattern) == true).ToListAsync(token);
    }
#endif
    
}