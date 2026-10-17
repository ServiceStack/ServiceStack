using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;
using ServiceStack.VirtualPath;

namespace ServiceStack.IO;

public class FileSystemVirtualFiles
    : AbstractVirtualPathProviderBase, IVirtualFiles
{
    public DirectoryInfo RootDirInfo { get; protected set; }
    protected FileSystemVirtualDirectory RootDir;

    public override IVirtualDirectory RootDirectory => RootDir;
    public override string VirtualPathSeparator => "/";
    public override string RealPathSeparator => Convert.ToString(Path.DirectorySeparatorChar);

    public FileSystemVirtualFiles(string rootDirectoryPath)
        : this(new DirectoryInfo(rootDirectoryPath))
    {
    }

    public FileSystemVirtualFiles(DirectoryInfo rootDirInfo)
    {
        this.RootDirInfo = rootDirInfo ?? throw new ArgumentNullException(nameof(rootDirInfo));
        Initialize();
    }

    protected sealed override void Initialize()
    {
        if (!RootDirInfo.Exists)
            throw new Exception($"RootDir '{RootDirInfo.FullName}' for virtual path does not exist");

        RootDir = new FileSystemVirtualDirectory(this, null, RootDirInfo);
    }

    public override bool DirectoryExists(string virtualPath)
    {
        var isDirectory = Directory.Exists(GetSafePath(virtualPath));
        return isDirectory;
    }

    public override bool FileExists(string virtualPath)
    {
        var isFile = File.Exists(GetSafePath(virtualPath));
        return isFile;
    }

    public override IVirtualFile GetFile(string virtualPath)
    {
        var realPath = GetSafePath(virtualPath);
        return base.GetFile(ToVirtualPath(realPath));
    }

    public override IVirtualDirectory GetDirectory(string virtualPath)
    {
        var realPath = GetSafePath(virtualPath);
        return base.GetDirectory(ToVirtualPath(realPath));
    }

    private string ToVirtualPath(string realPath) =>
        realPath.Substring(Path.GetFullPath(RootDir.RealPath).TrimEnd(Path.DirectorySeparatorChar).Length)
            .TrimStart(Path.DirectorySeparatorChar).Replace('\\', '/');

    private string GetSafePath(string virtualPath)
    {
        // Leading separators denote a VFS-rooted path, not an absolute filesystem path.
        var relativePath = SanitizePath(virtualPath) ?? "";
        var realPath = ResolveSafePath(RootDir.RealPath, relativePath);
        AssertNoLinks(RootDir.RealPath, realPath);
        return realPath;
    }

    private static void AssertNoLinks(string rootPath, string realPath)
    {
        // The configured root is trusted. Links below it are not supported: lexical
        // containment cannot prevent them redirecting IO outside the root. This check
        // assumes untrusted local processes cannot replace directories during an operation.
        var root = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar);
        var current = realPath;
        while (!string.Equals(current.TrimEnd(Path.DirectorySeparatorChar), root, PathComparison))
        {
            try
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new ArgumentException("Symbolic links are not allowed in virtual file paths.", "virtualPath");
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
            current = Path.GetDirectoryName(current);
        }
    }

    private static void AssertNoTreeLinks(string directory)
    {
        foreach (var child in Directory.GetFileSystemEntries(directory))
        {
            var attributes = File.GetAttributes(child);
            if ((attributes & FileAttributes.ReparsePoint) != 0)
                throw new ArgumentException("Symbolic links are not allowed in virtual file paths.", "dirPath");
            if ((attributes & FileAttributes.Directory) != 0)
                AssertNoTreeLinks(child);
        }
    }

    public string EnsureDirectory(string dirPath)
    {
        if (!Directory.Exists(dirPath))
            Directory.CreateDirectory(dirPath);

        return dirPath;
    }

    public void WriteFile(string filePath, string textContents)
    {
        var realFilePath = GetSafePath(filePath);
        EnsureDirectory(Path.GetDirectoryName(realFilePath));
        File.WriteAllText(realFilePath, textContents);
    }

    public void WriteFile(string filePath, Stream stream)
    {
        var realFilePath = GetSafePath(filePath);
        EnsureDirectory(Path.GetDirectoryName(realFilePath));
        using var fs = File.Open(realFilePath, FileMode.Create, FileAccess.Write);
        stream.WriteTo(fs);
    }

    public override async Task WriteFileAsync(string filePath, object contents, CancellationToken token=default)
    {
        if (contents == null)
            return;

        var realFilePath = GetSafePath(filePath);
        EnsureDirectory(Path.GetDirectoryName(realFilePath));
        using var fs = File.Open(realFilePath, FileMode.Create, FileAccess.Write);

        if (contents is IVirtualFile vfile)
            await WriteFileAsync(filePath, vfile.GetContents(), token).ConfigAwait();
        else if (contents is string textContents)
            await fs.WriteAsync(textContents, token).ConfigAwait();
        else if (contents is ReadOnlyMemory<char> romChars)
            await fs.WriteAsync(romChars.Span, token).ConfigAwait();
        else if (contents is byte[] binaryContents)
            await fs.WriteAsync(binaryContents, token: token).ConfigAwait();
        else if (contents is ReadOnlyMemory<byte> romBytes)
            await fs.WriteAsync(romBytes, token).ConfigAwait();
        else if (contents is Stream stream)
            await stream.CopyToAsync(fs, token).ConfigAwait();
        else
            throw CreateContentNotSupportedException(contents);
    }

    public void WriteFiles(IEnumerable<IVirtualFile> files, Func<IVirtualFile, string> toPath = null)
    {
        this.CopyFrom(files, toPath);
    }

    public void AppendFile(string filePath, string textContents)
    {
        var realFilePath = GetSafePath(filePath);
        EnsureDirectory(Path.GetDirectoryName(realFilePath));
        File.AppendAllText(realFilePath, textContents);
    }

    public void AppendFile(string filePath, Stream stream)
    {
        var realFilePath = GetSafePath(filePath);
        EnsureDirectory(Path.GetDirectoryName(realFilePath));
        using (var fs = new FileStream(realFilePath, FileMode.Append))
        {
            stream.WriteTo(fs);
        }
    }

    public void DeleteFile(string filePath)
    {
        var realFilePath = GetSafePath(filePath);
        try
        {
            File.Delete(realFilePath);
        }
        catch (Exception /*ignore*/)
        {
        }
    }

    public void DeleteFiles(IEnumerable<string> filePaths)
    {
        if (filePaths == null) return;
        filePaths.Each(DeleteFile);
    }

    public void DeleteFolder(string dirPath)
    {
        var realPath = GetSafePath(dirPath);
        if (string.IsNullOrEmpty(ToVirtualPath(realPath)))
            throw new ArgumentException("Cannot delete the virtual file root.", nameof(dirPath));
        if (Directory.Exists(realPath))
            AssertNoTreeLinks(realPath);
#if NETCORE
            // Doesn't properly recursively delete nested dirs/files on .NET Core (win at least)
            if (Directory.Exists(realPath))
                DeleteDirectory(realPath);
#else
        if (Directory.Exists(realPath))
            Directory.Delete(realPath, recursive: true);
#endif
    }
        
    public static void DeleteDirectory(string path)
    {
        if (!Directory.Exists(path))
            return;
            
        //modified from https://stackoverflow.com/a/1703799/85785
        foreach (var directory in Directory.GetDirectories(path))
        {
            var files = Directory.GetFiles(directory);
            foreach (var file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            DeleteDirectory(directory);
        }

        try
        {
            Directory.Delete(path, true);
        }
        catch (IOException) 
        {
            Directory.Delete(path, true);
        }
        catch (UnauthorizedAccessException)
        {
            Directory.Delete(path, true);
        }
    }
        
    public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
    {
        if (source == null || target == null)
            return;

        if (string.Equals(source.FullName, target.FullName, StringComparison.CurrentCultureIgnoreCase))
            return;

        // Check if the target directory exists, if not, create it.
        if (!Directory.Exists(target.FullName))
        {
            Directory.CreateDirectory(target.FullName);
        }

        // Copy each file into it's new directory.
        foreach (var file in source.GetFiles())
        {
            file.CopyTo(Path.Combine(target.ToString(), file.Name), true);
        }

        // Copy each subdirectory using recursion.
        foreach (var sourceSubDir in source.GetDirectories())
        {
            var nextTargetSubDir = target.CreateSubdirectory(sourceSubDir.Name);
            CopyAll(sourceSubDir, nextTargetSubDir);
        }
    }        

    public static string AssertDirectory(string dirPath, int timeoutMs=1000)
    {
        if (string.IsNullOrEmpty(dirPath))
            return null;

        try
        {
            ExecUtils.RetryOnException(() => {
                if (!Directory.Exists(dirPath))
                    Directory.CreateDirectory(dirPath);
            }, TimeSpan.FromMilliseconds(timeoutMs));
            return dirPath;
        }
        catch (TimeoutException e)
        {
            throw e.InnerException ?? e;
        }
    }

    public static void RecreateDirectory(string dirPath, int timeoutMs = 1000)
    {
        if (Directory.Exists(dirPath))
        {
            DeleteDirectory(dirPath);
        }
        AssertDirectory(dirPath, timeoutMs);
    }

    private static StringComparison PathComparison => Path.DirectorySeparatorChar == '\\'
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    private static string ResolveSafePath(string basePath, string relativePath)
    {
        if (string.IsNullOrEmpty(basePath) || relativePath == null)
            throw new ArgumentException("Invalid virtual file path.", nameof(relativePath));

        relativePath = relativePath.Replace('\\', '/');
        if (relativePath.StartsWith("/", StringComparison.Ordinal) || relativePath.IndexOf(':') >= 0 ||
            relativePath.IndexOf('\0') >= 0)
            throw new ArgumentException("Invalid virtual file path.", nameof(relativePath));

        // Windows can strip trailing dots/spaces when opening a path. Reject these
        // aliases so the path checked here is the path the filesystem will use.
        if (Path.DirectorySeparatorChar == '\\')
        {
            foreach (var segment in relativePath.Split('/'))
            {
                if (segment != "." && segment != ".." &&
                    (segment.EndsWith(".", StringComparison.Ordinal) || segment.EndsWith(" ", StringComparison.Ordinal)))
                    throw new ArgumentException("Invalid virtual file path.", nameof(relativePath));
            }
        }

        basePath = Path.GetFullPath(basePath);
        if (basePath.Length > Path.GetPathRoot(basePath).Length)
            basePath = basePath.TrimEnd(Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(basePath, relativePath));
        var basePathWithSep = basePath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
            ? basePath
            : basePath + Path.DirectorySeparatorChar;
        if (!string.Equals(fullPath, basePath, PathComparison) &&
            !fullPath.StartsWith(basePathWithSep, PathComparison))
            throw new ArgumentException("Virtual file path escapes the root directory.", nameof(relativePath));
        return fullPath;
    }

    /// <summary>
    /// Validates lexical containment of a relative path, normalizing both separator forms.
    /// Does not resolve symbolic links. File operations separately reject links below the root.
    /// </summary>
    public static bool IsPathSafe(string basePath, string relativePath)
    {
        try
        {
            ResolveSafePath(basePath, relativePath);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException ||
                                   ex is NotSupportedException ||
                                   ex is PathTooLongException)
        {
            return false;
        }
    }
}
