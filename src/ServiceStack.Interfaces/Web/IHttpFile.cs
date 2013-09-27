using System.IO;

namespace ServiceStack.Web
{
	public interface IHttpFile
	{
		string FileName { get; }
		long ContentLength { get; }
		string ContentType { get; }
		Stream InputStream { get; }
	}
}