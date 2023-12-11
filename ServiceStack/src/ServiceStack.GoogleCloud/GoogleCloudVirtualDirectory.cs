using Google.Cloud.Storage.V1;
using ServiceStack.IO;
using ServiceStack.Text;
using ServiceStack.VirtualPath;

namespace ServiceStack.GoogleCloud;

public class GoogleCloudVirtualDirectory : AbstractVirtualDirectoryBase
{
    internal GoogleCloudVirtualFiles PathProvider { get; private set; }

    public GoogleCloudVirtualDirectory(GoogleCloudVirtualFiles pathProvider, string dirPath, GoogleCloudVirtualDirectory? parentDir)
        : base(pathProvider, parentDir)
    {
        this.PathProvider = pathProvider;
        this.DirPath = dirPath;
    }
        
    static readonly char DirSep = '/';

    public DateTime DirLastModified { get; set; }

    public override DateTime LastModified => DirLastModified;

    public override IEnumerable<IVirtualFile> Files => PathProvider.GetImmediateFiles(DirPath);

    public override IEnumerable<IVirtualDirectory> Directories => PathProvider.GetImmediateDirectories(DirPath);

    public StorageClient Client => PathProvider.StorageClient;

    public string BucketName => PathProvider.BucketName;

    public string DirPath { get; set; }

    public override string VirtualPath => DirPath;

    public override string? Name => DirPath?.SplitOnLast(MemoryVirtualFiles.DirSep).Last();

    public override IVirtualFile? GetFile(string virtualPath)
    {
        var response = Client.GetObject(bucket:BucketName,
            objectName:DirPath.CombineWith(virtualPath));

        if (response == null)
            return null;
            
        return new GoogleCloudVirtualFile(PathProvider, this).Init(response);
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
        foreach (var file in PathProvider.GetImmediateFiles(DirPath).Where(f => f.Name.Glob(pattern)))
        {
            yield return file;
        }
    }

#if NET6_0_OR_GREATER
    public async IAsyncEnumerable<GoogleCloudVirtualFile> EnumerateFilesAsync(string pattern, CancellationToken token=default)
    {
        foreach (var file in await PathProvider.GetImmediateFilesAsync(DirPath, token).Where(f => f.Name.Glob(pattern)).ToListAsync(token))
        {
            yield return file;
        }
    }
#endif

    protected override IVirtualDirectory GetDirectoryFromBackingDirectoryOrDefault(string directoryName)
    {
        return new GoogleCloudVirtualDirectory(PathProvider, PathProvider.SanitizePath(DirPath.CombineWith(directoryName))!, this);
    }

    public void AddFile(string filePath, string contents)
    {
        using var ms = new MemoryStream();
        MemoryProvider.Instance.WriteUtf8ToStream(contents, ms);
        AddFile(filePath, ms);
    }

    public void AddFile(string filePath, Stream stream)
    {
        Client.UploadObject(bucket: PathProvider.BucketName, 
            source:stream, 
            objectName: StripDirSeparatorPrefix(filePath),
            contentType: MimeTypes.GetMimeType(filePath));
    }

    private static string StripDirSeparatorPrefix(string filePath)
    {
        return string.IsNullOrEmpty(filePath)
            ? filePath
            : (filePath[0] == DirSep ? filePath.Substring(1) : filePath);
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
        
#if NET6_0_OR_GREATER
    public virtual async Task<List<GoogleCloudVirtualFile>> GetAllMatchingFilesAsync(string globPattern, int maxDepth = int.MaxValue, 
        CancellationToken token = default)
    {
        if (IsRoot)
        {
            return await PathProvider.EnumerateFilesAsync(token:token).Where(x => 
                (x.DirPath == null || x.DirPath.CountOccurrencesOf('/') < maxDepth-1)
                && x.Name.Glob(globPattern)).ToListAsync(token);
        }
            
        return await PathProvider.EnumerateFilesAsync(DirPath, token).Where(x => 
            x.DirPath != null
            && x.DirPath.CountOccurrencesOf('/') < maxDepth-1
            && x.DirPath.StartsWith(DirPath)
            && x.Name.Glob(globPattern)).ToListAsync(token);
    }
#endif
    
}