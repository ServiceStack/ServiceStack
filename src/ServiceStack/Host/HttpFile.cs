using System.IO;
using ServiceStack.Server;

namespace ServiceStack.Host
{
	public class HttpFile : IFile
	{
		public string FileName { get; set; }
		public long ContentLength { get; set; }
		public string ContentType { get; set; }
		public Stream InputStream { get; set; }
	}
}