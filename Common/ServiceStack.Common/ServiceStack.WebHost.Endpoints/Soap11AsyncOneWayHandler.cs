using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Metadata;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.WebHost.Endpoints
{
	public class Soap11AsyncOneWayHandler 
		: SoapHandler, IHttpHandler
	{
		public override EndpointAttributes SoapType
		{
			get { return EndpointAttributes.Soap11; }
		}

		public void ProcessRequest(HttpContext context)
		{
			if (context.Request.HttpMethod == HttpMethods.Get)
			{
				var wsdl = new Soap11WsdlMetadataHandler();
				wsdl.Execute(context);
				return;
			}

			var requestMessage = GetSoap11RequestMessage(context);
			SendOneWay(requestMessage);
		}

		public bool IsReusable
		{
			get { return true; }
		}
	}
}