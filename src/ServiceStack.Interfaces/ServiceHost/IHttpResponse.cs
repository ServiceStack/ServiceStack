using System.IO;

namespace ServiceStack.ServiceHost
{
	public interface IHttpResponse
	{
		int StatusCode { set; }

        string StatusDescription { set; }

		string ContentType { get; set; }

		void AddHeader(string name, string value);

		void Redirect(string url);

		Stream OutputStream { get; }

		void Write(string text);

		/// <summary>
		/// Signal that this response has been handled and no more processing should be done
		/// </summary>
		void Close();

		/// <summary>
		/// Gets a value indicating whether this instance is closed.
		/// </summary>
		bool IsClosed { get; }
	}
}