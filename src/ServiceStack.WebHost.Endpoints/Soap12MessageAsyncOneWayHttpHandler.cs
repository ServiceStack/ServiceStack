using System.Web;
using System.Xml;
using ServiceStack.Common.Web;
using ServiceStack.WebHost.Endpoints.Metadata;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.WebHost.Endpoints
{
	public class Soap12MessageAsyncOneWayHttpHandler 
		: SoapHandler, IHttpHandler
	{
		public void ProcessRequest(HttpContext context)
		{
			if (context.Request.HttpMethod == HttpMethods.Get)
			{
				var wsdl = new Soap12WsdlMetadataHandler();
				wsdl.Execute(context);
				return;
			}

			var requestMessage = GetSoap12RequestMessage(context);
			SendOneWay(requestMessage);
		}

		public bool IsReusable
		{
			get { return true; }
		}
	}
}