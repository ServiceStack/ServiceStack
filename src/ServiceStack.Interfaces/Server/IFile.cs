using System.IO;

namespace ServiceStack.Server
{
	public interface IFile
	{
		string FileName { get; }
		long ContentLength { get; }
		string ContentType { get; }
		Stream InputStream { get; }
	}
}