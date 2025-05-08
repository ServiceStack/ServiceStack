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

    public static void WriteFile(this IVirtualPathProvider pathProvider, string filePath, string textContents)
    {
        if (!(pathProvider is IVirtualFiles writableFs))
            throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

        writableFs.WriteFile(filePath, textContents);
    }

    public static void WriteFile(this IVirtualPathProvider pathProvider, string filePath, Stream stream)
    {
        if (!(pathProvider is IVirtualFiles writableFs))
            throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

        writableFs.WriteFile(filePath, stream);
    }

    public static void WriteFile(this IVirtualPathProvider pathProvider, string filePath, byte[] bytes)
    {
        if (!(pathProvider is IVirtualFiles writableFs))
            throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

        using (var ms = MemoryStreamFactory.GetStream(bytes))
        {
            writableFs.WriteFile(filePath, ms);
        }
    }

    public static void WriteFile(this IVirtualPathProvider pathProvider, string filePath, ReadOnlyMemory<char> text)
    {
        if (!(pathProvider is AbstractVirtualPathProviderBase writableFs))
            throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

        writableFs.WriteFile(filePath, text);
    }

    public static void WriteFile(this IVirtualPathProvider pathProvider, string filePath, ReadOnlyMemory<byte> bytes)
    {
        if (!(pathProvider is AbstractVirtualPathProviderBase writableFs))
            throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

        writableFs.WriteFile(filePath, bytes);
    }

    public static void WriteFile(this IVirtualPathProvider pathProvider, string filePath, object contents)
    {
        if (!(pathProvider is IVirtualFiles writableFs))
            throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

        writableFs.WriteFile(filePath, contents);
    }

    public static void AppendFile(this IVirtualPathProvider pathProvider, string filePath, string textContents)
    {
        if (!(pathProvider is IVirtualFiles writableFs))
            throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

        writableFs.AppendFile(filePath, textContents);
    }

    public static void AppendFile(this IVirtualPathProvider pathProvider, string filePath, Stream stream)
    {
        if (!(pathProvider is IVirtualFiles writableFs))
            throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

        writableFs.AppendFile(filePath, stream);
    }

    public static void AppendFile(this IVirtualPathProvider pathProvider, string filePath, byte[] bytes)
    {
        if (!(pathProvider is IVirtualFiles writableFs))
            throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

        using (var ms = MemoryStreamFactory.GetStream(bytes))
        {
            writableFs.AppendFile(filePath, ms);
        }
    }

    public static void AppendFile(this IVirtualPathProvider pathProvider, string filePath, object contents)
    {
        if (!(pathProvider is IVirtualFiles writableFs))
            throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

        writableFs.AppendFile(filePath, contents);
    }

    public static void AppendFile(this IVirtualPathProvider pathProvider, string filePath, ReadOnlyMemory<char> text)
    {
        if (!(pathProvider is AbstractVirtualPathProviderBase writableFs))
            throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

        writableFs.AppendFile(filePath, text);
    }

    public static void AppendFile(this IVirtualPathProvider pathProvider, string filePath, ReadOnlyMemory<byte> bytes)
    {
        if (!(pathProvider is AbstractVirtualPathProviderBase writableFs))
            throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

        writableFs.AppendFile(filePath, bytes);
    }

    public static void WriteFile(this IVirtualPathProvider pathProvider, IVirtualFile file, string filePath = null)
    {
        if (!(pathProvider is IVirtualFiles writableFs))
            throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

        using (var stream = file.OpenRead())
        {
            writableFs.WriteFile(filePath ?? file.VirtualPath, stream);
        }
    }

    public static void DeleteFile(this IVirtualPathProvider pathProvider, string filePath)
    {
        if (!(pathProvider is IVirtualFiles writableFs))
            throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

        writableFs.DeleteFile(filePath);
    }

    public static void DeleteFile(this IVirtualPathProvider pathProvider, IVirtualFile file)
    {
        pathProvider.DeleteFile(file.VirtualPath);
    }

    public static void DeleteFiles(this IVirtualPathProvider pathProvider, IEnumerable<string> filePaths)
    {
        if (!(pathProvider is IVirtualFiles writableFs))
            throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

        writableFs.DeleteFiles(filePaths);
    }

    public static void DeleteFiles(this IVirtualPathProvider pathProvider, IEnumerable<IVirtualFile> files)
    {
        if (!(pathProvider is IVirtualFiles writableFs))
            throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

        writableFs.DeleteFiles(files.Map(x => x.VirtualPath));
    }

    public static void DeleteFolder(this IVirtualPathProvider pathProvider, string dirPath)
    {
        if (!(pathProvider is IVirtualFiles writableFs))
            throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

        writableFs.DeleteFolder(dirPath);
    }

    public static void WriteFiles(this IVirtualPathProvider pathProvider, Dictionary<string, string> textFiles)
    {
        if (!(pathProvider is IVirtualFiles writableFs))
            throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

        writableFs.WriteFiles(textFiles);
    }

    public static void WriteFiles(this IVirtualPathProvider pathProvider, Dictionary<string, object> files)
    {
        if (!(pathProvider is IVirtualFiles writableFs))
            throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

        writableFs.WriteFiles(files);
    }

    public static void WriteFiles(this IVirtualPathProvider pathProvider, IEnumerable<IVirtualFile> srcFiles, Func<IVirtualFile, string> toPath = null)
    {
        if (!(pathProvider is IVirtualFiles writableFs))
            throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

        writableFs.WriteFiles(srcFiles, toPath);
    }

    public static void CopyFrom(this IVirtualPathProvider pathProvider, IEnumerable<IVirtualFile> srcFiles, Func<IVirtualFile, string> toPath=null)
    {
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
