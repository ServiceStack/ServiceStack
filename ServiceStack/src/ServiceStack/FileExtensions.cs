using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.IO;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack;

public static class FileExtensions
{
    public static void SaveTo(this IHttpFile httpFile, string filePath)
    {
        using var sw = new StreamWriter(File.Create(filePath));
        httpFile.InputStream.WriteTo(sw.BaseStream);
    }

    public static void SaveTo(this IHttpFile httpFile, IVirtualFiles vfs, string filePath)
    {
        vfs.WriteFile(filePath, httpFile.InputStream);
    }

    public static void WriteTo(this IHttpFile httpFile, Stream stream)
    {
        httpFile.InputStream.WriteTo(stream);
    }

    public static async Task SaveToAsync(this IHttpFile httpFile, string filePath)
    {
#if NET6_0_OR_GREATER
        await
#endif
            using var sw = new StreamWriter(File.Create(filePath));
        await httpFile.InputStream.WriteToAsync(sw.BaseStream).ConfigAwait();
    }

    public static async Task SaveToAsync(this IHttpFile httpFile, IVirtualFiles vfs, string filePath, CancellationToken token=default)
    {
        await vfs.WriteFileAsync(filePath, httpFile.InputStream, token).ConfigAwait();
    }

    public static async Task WriteToAsync(this IHttpFile httpFile, Stream stream)
    {
        await httpFile.InputStream.WriteToAsync(stream).ConfigAwait();
    }

    public static string MapServerPath(this string relativePath)
    {
        return HostContext.IsAspNetHost
            ? relativePath.MapHostAbsolutePath()
            : relativePath.MapAbsolutePath();
    }

    public static bool IsRelativePath(this string relativeOrAbsolutePath)
    {
        return !relativeOrAbsolutePath.Contains(":")
               && !relativeOrAbsolutePath.StartsWith("/")
               && !relativeOrAbsolutePath.StartsWith("\\");
    }

    public static byte[] ReadFully(this FileInfo file)
    {
        using var fs = file.OpenRead();
        return fs.ReadFully();
    }

    public static async Task<byte[]> ReadFullyAsync(this FileInfo file, CancellationToken token=default)
    {
#if NET6_0_OR_GREATER
        return await File.ReadAllBytesAsync(file.FullName, token).ConfigAwait();
#else
            using var fs = file.OpenRead();
            return await fs.ReadFullyAsync(token).ConfigAwait();
#endif
    }

    public static string ReadAllText(this FileInfo file)
    {
        return file.ReadFully().FromUtf8Bytes();
    }

    public static async Task<string> ReadAllTextAsync(this FileInfo file, CancellationToken token=default)
    {
#if NET6_0_OR_GREATER
        return await File.ReadAllTextAsync(file.FullName, token).ConfigAwait();
#else
            return (await file.ReadFullyAsync(token).ConfigAwait()).FromUtf8Bytes();
#endif
    }
}