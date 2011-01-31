using System.IO;
using ServiceStack.Text;

namespace ServiceStack.ServiceHost
{
	public static class FileExtensions
	{
		public static void SaveTo(this IFile file, string filePath)
		{
			using (var sw = new StreamWriter(filePath, false))
			{
				file.InputStream.WriteTo(sw.BaseStream);
			}
		}

		public static void WriteTo(this IFile file, Stream stream)
		{
			file.InputStream.WriteTo(stream);
		}
	}
}