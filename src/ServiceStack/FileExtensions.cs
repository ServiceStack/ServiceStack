using System.IO;
using System.Web;
using ServiceStack.Support.WebHost;
using ServiceStack.Text;
using ServiceStack.Utils;
using ServiceStack.Web;
using ServiceStack.Web.HttpListener;

namespace ServiceStack
{
	public static class FileExtensions
	{
		public static void SaveTo(this IHttpFile httpFile, string filePath)
		{
			using (var sw = new StreamWriter(filePath, false))
			{
				httpFile.InputStream.WriteTo(sw.BaseStream);
			}
		}

		public static void WriteTo(this IHttpFile httpFile, Stream stream)
		{
			httpFile.InputStream.WriteTo(stream);
		}

		public static string MapServerPath(this string relativePath)
		{
			var isAspNetHost = HttpListenerBase.Instance == null || HttpContext.Current != null;
			var appHost = EndpointHost.AppHost;
			if (appHost != null)
			{
				isAspNetHost = !(appHost is HttpListenerBase);
			}

			return isAspNetHost
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
            using (var fs = file.OpenRead())
            {
                return fs.ReadFully();
            }
        }
	}
}