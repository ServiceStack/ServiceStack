using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.IO;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    public static class FileExtensions
    {
        public static void SaveTo(this IHttpFile httpFile, string filePath)
        {
            using (var sw = new StreamWriter(File.Create(filePath)))
            {
                httpFile.InputStream.WriteTo(sw.BaseStream);
            }
        }

        public static void SaveTo(this IHttpFile httpFile, IVirtualFiles vfs, string filePath)
        {
            vfs.WriteFile(filePath, httpFile.InputStream);
        }

        public static void WriteTo(this IHttpFile httpFile, Stream stream)
        {
            httpFile.InputStream.WriteTo(stream);
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

        public static Task<byte[]> ReadFullyAsync(this FileInfo file, CancellationToken token=new())
        {
            using var fs = file.OpenRead();
            return fs.ReadFullyAsync(token);
        }

        public static string ReadAllText(this FileInfo file)
        {
            return file.ReadFully().FromUtf8Bytes();
        }

        public static async Task<string> ReadAllTextAsync(this FileInfo file, CancellationToken token=new())
        {
            return (await file.ReadFullyAsync(token)).FromUtf8Bytes();
        }
    }
}