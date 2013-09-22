using System.IO;

namespace ServiceStack.Server
{
	public interface IStreamWriter
	{
		void WriteTo(Stream responseStream);
	}
}