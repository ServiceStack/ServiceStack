using System.IO;

namespace ServiceStack.ServiceHost
{
	public interface IHttpResponse
	{
		int StatusCode { set; }

		string ContentType { get; set; }

		void AddHeader(string name, string value);

		Stream OutputStream { get; }

		void Write(string text);
	}
}