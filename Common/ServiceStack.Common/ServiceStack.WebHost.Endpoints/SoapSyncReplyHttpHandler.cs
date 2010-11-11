using System.IO;
using System.ServiceModel.Channels;
using System.Web;
using System.Xml;
using ServiceStack.Common.Web;
using ServiceStack.WebHost.Endpoints.Metadata;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.WebHost.Endpoints
{
	public class SoapSyncReplyHttpHandler : SoapSyncReplyHandler, IHttpHandler
	{
		public void ProcessRequest(HttpContext context)
		{
			if (context.Request.HttpMethod == HttpMethods.Get)
			{
				var wsdl = new Soap12WsdlMetadataHandler();
				wsdl.Execute(context);
				return;
			}

			var requestMessage = GetRequestMessage(context);
			var responseMessage = Send(requestMessage);
			
			context.Response.ContentType = GetSoapContentType(context);
			using (var writer = XmlWriter.Create(context.Response.OutputStream))
			{
				responseMessage.WriteMessage(writer);
			}
		}

		public bool IsReusable
		{
			get { return true; }
		}
	}
}