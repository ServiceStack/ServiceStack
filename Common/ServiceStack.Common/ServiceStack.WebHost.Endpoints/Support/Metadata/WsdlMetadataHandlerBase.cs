using System;
using System.Web;
using ServiceStack.WebHost.Endpoints.Metadata;
using ServiceStack.WebHost.Endpoints.Support.Templates;

namespace ServiceStack.WebHost.Endpoints.Support.Metadata
{
	public abstract class WsdlMetadataHandlerBase : HttpHandlerBase
	{
		protected abstract WsdlTemplateBase GetWsdlTemplate();

		public override void Execute(HttpContext context)
		{
			context.Response.ContentType = "text/xml";
			var operations = EndpointHost.ServiceOperations;

			var baseUri = GetBaseUri(context.Request);
			var xsd = new XsdGenerator {
				OperationTypes = operations.AllOperations.Types,
				OptimizeForFlash = context.Request.QueryString["flash"] != null,
				IncludeAllTypesInAssembly = context.Request.QueryString["includeAllTypes"] != null,
			}.ToString();

			var wsdlTemplate = GetWsdlTemplate();
			wsdlTemplate.Xsd = xsd;
			wsdlTemplate.ReplyOperationNames = EndpointHost.ServiceOperations.ReplyOperations.Names;
			wsdlTemplate.OneWayOperationNames = operations.OneWayOperations.Names;
			wsdlTemplate.ReplyEndpointUri = baseUri + "SyncReply.svc";
			wsdlTemplate.OneWayEndpointUri = baseUri + "AsyncOneWay.svc";

			context.Response.Write(wsdlTemplate.ToString());
		}

		public string GetBaseUri(HttpRequest request)
		{
			var appPath = request.Url.AbsolutePath;
			var endpointsPath = appPath.Substring(0, appPath.LastIndexOf('/') + 1);
			return request.Url.GetLeftPart(UriPartial.Authority) + endpointsPath;
		}
	}
}