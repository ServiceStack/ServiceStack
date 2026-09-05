using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.IO;
using ServiceStack.Text;
using ServiceStack.VirtualPath;

namespace ServiceStack.IO;

public static class VirtualFilesExtensions
{
    private const string ErrorNotWritable = "{0} does not implement IVirtualFiles";

    public static bool IsFile(this IVirtualPathProvider pathProvider, string filePath)
    {
        return pathProvider.FileExists(filePath);
    }

    public static bool IsDirectory(this IVirtualPathProvider pathProvider, string filePath)
    {
        return pathProvider.DirectoryExists(filePath);
    }

    private static IVirtualFiles AssertWritable(IVirtualPathProvider pathProvider)
    {
        if (pathProvider == null)
            throw new ArgumentNullException(nameof(pathProvider));
        if (pathProvider is not IVirtualFiles writableFs)
            throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));
        return writableFs;
    }

    private static AbstractVirtualPathProviderBase AssertAbstractWritable(IVirtualPathProvider pathProvider)
    {
        if (pathProvider == null)
            throw new ArgumentNullException(nameof(pathProvider));
        if (pathProvider is not AbstractVirtualPathProviderBase writableFs)
            throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));
        return writableFs;
    }

    public static void WriteFile(this IVirtualPathProvider pathProvider, string filePath, string textContents)
    {
        AssertWritable(pathProvider).WriteFile(filePath, textContents);
    }

    public static void WriteFile(this IVirtualPathProvider pathProvider, string filePath, Stream stream)
    {
        AssertWritable(pathProvider).WriteFile(filePath, stream);
    }

    public static void WriteFile(this IVirtualPathProvider pathProvider, string filePath, byte[] bytes)
    {
        using var ms = MemoryStreamFactory.GetStream(bytes);
        AssertWritable(pathProvider).WriteFile(filePath, ms);
    }

    public static void WriteFile(this IVirtualPathProvider pathProvider, string filePath, ReadOnlyMemory<char> text)
    {
        AssertAbstractWritable(pathProvider).WriteFile(filePath, text);
    }

    public static void WriteFile(this IVirtualPathProvider pathProvider, string filePath, ReadOnlyMemory<byte> bytes)
    {
        AssertAbstractWritable(pathProvider).WriteFile(filePath, bytes);
    }

    public static void WriteFile(this IVirtualPathProvider pathProvider, string filePath, object contents)
    {
        AssertWritable(pathProvider).WriteFile(filePath, contents);
    }

    public static void AppendFile(this IVirtualPathProvider pathProvider, string filePath, string textContents)
    {
        AssertWritable(pathProvider).AppendFile(filePath, textContents);
    }

    public static void AppendFile(this IVirtualPathProvider pathProvider, string filePath, Stream stream)
    {
        AssertWritable(pathProvider).AppendFile(filePath, stream);
    }

    public static void AppendFile(this IVirtualPathProvider pathProvider, string filePath, byte[] bytes)
    {
        using var ms = MemoryStreamFactory.GetStream(bytes);
        AssertWritable(pathProvider).AppendFile(filePath, ms);
    }

    public static void AppendFile(this IVirtualPathProvider pathProvider, string filePath, object contents)
    {
        AssertWritable(pathProvider).AppendFile(filePath, contents);
    }

    public static void AppendFile(this IVirtualPathProvider pathProvider, string filePath, ReadOnlyMemory<char> text)
    {
        AssertAbstractWritable(pathProvider).AppendFile(filePath, text);
    }

    public static void AppendFile(this IVirtualPathProvider pathProvider, string filePath, ReadOnlyMemory<byte> bytes)
    {
        AssertAbstractWritable(pathProvider).AppendFile(filePath, bytes);
    }

    public static void WriteFile(this IVirtualPathProvider pathProvider, IVirtualFile file, string filePath = null)
    {
        if (file == null)
            throw new ArgumentNullException(nameof(file));

        using var stream = file.OpenRead();
        AssertWritable(pathProvider).WriteFile(filePath ?? file.VirtualPath, stream);
    }

    public static void DeleteFile(this IVirtualPathProvider pathProvider, string filePath)
    {
        AssertWritable(pathProvider).DeleteFile(filePath);
    }

    public static void DeleteFile(this IVirtualPathProvider pathProvider, IVirtualFile file)
    {
        if (file == null)
            throw new ArgumentNullException(nameof(file));

        pathProvider.DeleteFile(file.VirtualPath);
    }

    public static void DeleteFiles(this IVirtualPathProvider pathProvider, IEnumerable<string> filePaths)
    {
        AssertWritable(pathProvider).DeleteFiles(filePaths);
    }

    public static void DeleteFiles(this IVirtualPathProvider pathProvider, IEnumerable<IVirtualFile> files)
    {
        if (files == null)
            return;

        AssertWritable(pathProvider).DeleteFiles(files.Map(x => x.VirtualPath));
    }

    public static void DeleteFolder(this IVirtualPathProvider pathProvider, string dirPath)
    {
        AssertWritable(pathProvider).DeleteFolder(dirPath);
    }

    public static void WriteFiles(this IVirtualPathProvider pathProvider, Dictionary<string, string> textFiles)
    {
        AssertWritable(pathProvider).WriteFiles(textFiles);
    }

    public static void WriteFiles(this IVirtualPathProvider pathProvider, Dictionary<string, object> files)
    {
        AssertWritable(pathProvider).WriteFiles(files);
    }

    public static void WriteFiles(this IVirtualPathProvider pathProvider, IEnumerable<IVirtualFile> srcFiles, Func<IVirtualFile, string> toPath = null)
    {
        AssertWritable(pathProvider).WriteFiles(srcFiles, toPath);
    }

    public static void CopyFrom(this IVirtualPathProvider pathProvider, IEnumerable<IVirtualFile> srcFiles, Func<IVirtualFile, string> toPath=null)
    {
        if (srcFiles == null)
            return;

        foreach (var file in srcFiles)
        {
            using var stream = file.OpenRead();
            var dstPath = toPath != null ? toPath(file) : file.VirtualPath;
            if (dstPath == null)
                continue;

            pathProvider.WriteFile(dstPath, stream);
        }
    }
}

public static class VirtualDirectoryExtensions
{
    /// <summary>
    /// Get only files in this directory
    /// </summary>
    /// <param name="dir"></param>
    /// <returns></returns>
    public static IEnumerable<IVirtualFile> GetFiles(this IVirtualDirectory dir)
    {
        return dir.Files;
    }

    /// <summary>
    /// Get only sub directories in this directory
    /// </summary>
    /// <param name="dir"></param>
    /// <returns></returns>
    public static IEnumerable<IVirtualDirectory> GetDirectories(this IVirtualDirectory dir)
    {
        return dir.Directories;
    }
        
    /// <summary>
    /// Get All Files in current and all sub directories
    /// </summary>
    /// <param name="dir"></param>
    /// <returns></returns>
    public static IEnumerable<IVirtualFile> GetAllFiles(this IVirtualDirectory dir)
    {
        if (dir != null)
        {
            foreach (var subDir in dir.GetDirectories())
            {
                foreach (var file in subDir.GetAllFiles())
                {
                    yield return file;
                }
            }

            foreach (var file in dir.Files)
            {
                yield return file;
            }
        }
    }
        
    // VFS Async providers only need implement, which all async APIs are routed to:
    // Task WriteFileAsync(string filePath, object contents, CancellationToken token=default);
    // E.g. see FileSystemVirtualFiles.WriteFileAsync()

    public static async Task WriteFileAsync(this IVirtualFiles vfs, string filePath, IVirtualFile file, CancellationToken token = default) =>
        await vfs.WriteFileAsync(filePath, file, token).ConfigAwait();
    public static async Task WriteFileAsync(this IVirtualFiles vfs, string filePath, string textContents, CancellationToken token = default) =>
        await vfs.WriteFileAsync(filePath, textContents, token).ConfigAwait();
    public static async Task WriteFileAsync(this IVirtualFiles vfs, string filePath, ReadOnlyMemory<char> textContents, CancellationToken token = default) =>
        await vfs.WriteFileAsync(filePath, textContents, token).ConfigAwait();
    public static async Task WriteFileAsync(this IVirtualFiles vfs, string filePath, byte[] binaryContents, CancellationToken token = default) =>
        await vfs.WriteFileAsync(filePath, binaryContents, token).ConfigAwait();
    public static async Task WriteFileAsync(this IVirtualFiles vfs, string filePath, ReadOnlyMemory<byte> romBytes, CancellationToken token = default) =>
        await vfs.WriteFileAsync(filePath, romBytes, token).ConfigAwait();
    public static async Task WriteFileAsync(this IVirtualFiles vfs, string filePath, Stream stream, CancellationToken token = default) =>
        await vfs.WriteFileAsync(filePath, stream, token).ConfigAwait();
}
