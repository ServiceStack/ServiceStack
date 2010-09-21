using System.IO;

namespace ServiceStack.Service
{
	public interface IStreamWriter
	{
		void WriteTo(Stream stream);
	}
}