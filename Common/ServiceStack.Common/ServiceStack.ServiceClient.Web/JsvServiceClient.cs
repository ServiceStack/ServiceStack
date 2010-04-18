using System;
using System.IO;
using ServiceStack.Text;

namespace ServiceStack.ServiceClient.Web
{
	public class JsvServiceClient
		: ServiceClientBase
	{
		public JsvServiceClient()
		{
		}

		/// <summary>
		/// Base Url of Service Stack's Web Service endpoints, i.e. http://localhost/Public/
		/// </summary>
		/// <param name="baseUri"></param>
		public JsvServiceClient(string baseUri) 
		{
			this.BaseUri = baseUri.WithTrailingSlash() + "Jsv/";
		}

		public JsvServiceClient(string syncReplyBaseUri, string asyncOneWayBaseUri) 
			: base(syncReplyBaseUri, asyncOneWayBaseUri)
		{
		}

		public override string ContentType
		{
			get { return "text/jsv"; }
		}

		public override void SerializeToStream(object request, Stream stream)
		{
			using (var writer = new StreamWriter(stream))
			{
				TypeSerializer.SerializeToWriter(request, writer);
			}
		}

		public override T DeserializeFromStream<T>(Stream stream)
		{
			using (var reader = new StreamReader(stream))
			{
				return TypeSerializer.DeserializeFromReader<T>(reader);
			}
		}

	}
}